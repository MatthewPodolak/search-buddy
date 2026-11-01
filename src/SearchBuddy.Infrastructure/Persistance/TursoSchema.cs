using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.Persistance
{
    internal class TursoSchema
    {
        public static readonly string[] Statements =
        [
            """
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INTEGER PRIMARY KEY,
                    Path TEXT NOT NULL UNIQUE,
                    ContentHash TEXT NOT NULL
                )
                """,
                """
                CREATE TABLE IF NOT EXISTS Chunks (
                    Id INTEGER PRIMARY KEY,
                    DocumentId INTEGER NOT NULL REFERENCES Documents(Id) ON DELETE CASCADE,
                    ChunkIndex INTEGER NOT NULL,
                    Text TEXT NOT NULL,
                    Embedding BLOB NOT NULL,
                    UNIQUE (DocumentId, ChunkIndex)
                )
                """
        ];
    }
}
