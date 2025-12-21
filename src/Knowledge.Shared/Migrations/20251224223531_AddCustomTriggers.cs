using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Knowledge.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create sequence for message ordering
            migrationBuilder.Sql(@"
                CREATE SEQUENCE IF NOT EXISTS messages_sequence_number_seq
                    START WITH 1
                    INCREMENT BY 1
                    NO MINVALUE
                    NO MAXVALUE
                    CACHE 1;
            ");

            // Create trigger function for auto-assigning sequence numbers
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION assign_message_sequence_number()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.sequence_number := nextval('messages_sequence_number_seq');
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create trigger on messages table
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trigger_assign_sequence_number ON messages;
                CREATE TRIGGER trigger_assign_sequence_number
                    BEFORE INSERT ON messages
                    FOR EACH ROW
                    EXECUTE FUNCTION assign_message_sequence_number();
            ");

            // Create HNSW index for vector similarity search
            // Using cosine distance for normalized embeddings (text-embedding-3-small)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_embeddings_vector_hnsw
                ON embeddings
                USING hnsw (vector vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop HNSW index
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_embeddings_vector_hnsw;");

            // Drop trigger
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_assign_sequence_number ON messages;");

            // Drop trigger function
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS assign_message_sequence_number();");

            // Drop sequence
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS messages_sequence_number_seq;");
        }
    }
}
