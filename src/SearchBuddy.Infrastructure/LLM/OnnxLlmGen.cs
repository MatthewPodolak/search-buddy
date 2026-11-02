
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.ML.OnnxRuntimeGenAI;
using SearchBuddy.Application.Contracts;
using SearchBuddy.Application.LLM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.LLM
{
    public class OnnxLlmGen : ILlmGen, IDisposable
    {
        private readonly Model _model;
        private readonly Tokenizer _tokenizer;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly string _systemPrompt =
        """
            Jesteś asystentem, który odpowiada na pytania o pliki użytkownika.
            Dostaniesz kilka fragmentów plików oznaczonych numerami [1], [2] itd.
            Zwykle tylko jeden lub dwa fragmenty dotyczą pytania, resztę pomiń.

            Znajdź fragment, który odpowiada na pytanie, i podaj odpowiedź wprost,
            z konkretami z tekstu (kwoty, daty, nazwy, miejsca).
            Pytanie może używać innych słów niż tekst, np. "papierosy" i "szlugi" to to samo.

            Odpowiadaj tylko na podstawie fragmentów. Jeśli żaden fragment nie dotyczy pytania,
            napisz, że nie znalazłeś tego w plikach.

            Na końcu podaj numery fragmentów, z których korzystałeś, np. [1].
        """;

        public OnnxLlmGen()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "models", "llm", "qwen-3-4b");

            _model = new Model(dir);
            _tokenizer = new Tokenizer(_model);
        }

        public async Task<string> AskAsync(string question, IReadOnlyList<SearchHit> relevantData, CancellationToken ct)
        {
            var context = new StringBuilder();
            int i = 1;

            foreach (var item in relevantData)
            {
                context.AppendLine($"[{i++}] Źródło: {item.Path}");
                context.AppendLine(item.Text);
                context.AppendLine();
            }

            string userPrompt = $"Teksty: {context} Pytanie: {question} Odpowiedz: ";

            var prompt =
                $"<|im_start|>system\n{_systemPrompt}<|im_end|>\n" +
                $"<|im_start|>user\n{userPrompt}<|im_end|>\n" +
                $"<|im_start|>assistant\n<think>\n\n</think>\n\n";

            await _gate.WaitAsync(ct);
            try
            {
                return await Task.Run(() => Generate(prompt, ct), ct);
            }
            finally
            {
                _gate.Release();
            }
        }

        private string Generate(string prompt, CancellationToken ct)
        {
            using var sequences = _tokenizer.Encode(prompt);
            using var genParams = new GeneratorParams(_model);
            genParams.SetSearchOption("max_length", 4096);

            using var generator = new Generator(_model, genParams);
            generator.AppendTokenSequences(sequences);

            using var stream = _tokenizer.CreateStream();
            var sb = new StringBuilder();

            while (!generator.IsDone())
            {
                ct.ThrowIfCancellationRequested();
                generator.GenerateNextToken();

                var seq = generator.GetSequence(0);
                sb.Append(stream.Decode(seq[^1]));
            }

            return sb.ToString().Trim();
        }

        public void Dispose()
        {
            _tokenizer.Dispose();
            _model.Dispose();
        }
    }
}
