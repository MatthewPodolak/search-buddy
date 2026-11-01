using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using SearchBuddy.Application.LLM;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.LLM
{
    public class Embedder : IEmbedder, IDisposable
    {
        private readonly InferenceSession _session;
        private readonly SentencePieceTokenizer _tokenizer;
        public Embedder() 
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "models", "embedding", "multilingual-e5-small");

            _session = new InferenceSession(Path.Combine(dir, "model.onnx"));

            using var stream = File.OpenRead(Path.Combine(dir, "sentencepiece.bpe.model"));
            _tokenizer = SentencePieceTokenizer.Create(stream, true, true);
        }
        public Task<ReadOnlyMemory<float>> EmbeddAsync(string text, bool isQuest, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            string input = isQuest ? $"query: {text}" : $"passage: {text}";

            var spm = _tokenizer.EncodeToIds(input);
            int contentLen = Math.Min(spm.Count - 2, 510);

            var ids = new long[contentLen + 2];
            ids[0] = 0;
            for (int i = 0; i < contentLen; i++)
            {
                int id = spm[i + 1];
                ids[i + 1] = id == 0 ? 3 : id + 1;
            }
            ids[^1] = 2;

            int len = ids.Length;

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(ids, [1, len])),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(Enumerable.Repeat(1L, len).ToArray(), [1, len]))
            };

            if (_session.InputMetadata.ContainsKey("token_type_ids"))
                inputs.Add(NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>([1, len])));

            using var outputs = _session.Run(inputs);
            var hidden = outputs.First().AsTensor<float>();
            int dim = hidden.Dimensions[2];

            var vector = new float[dim];
            for (int t = 0; t < len; t++)
            {
                for (int d = 0; d < dim; d++)
                {
                    vector[d] += hidden[0, t, d];
                }
            }


            float norm = 0;
            for (int d = 0; d < dim; d++) 
            {
                vector[d] /= len; norm += vector[d] * vector[d];
            }

            norm = MathF.Sqrt(norm);

            for (int d = 0; d < dim; d++)
            {
                vector[d] /= norm;
            }

            return Task.FromResult<ReadOnlyMemory<float>>(vector);
        }

        public void Dispose()
        {
            _session.Dispose();
        }
    }
}