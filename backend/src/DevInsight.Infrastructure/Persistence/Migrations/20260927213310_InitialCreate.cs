using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DevInsight.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_protection_keys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    friendly_name = table.Column<string>(type: "text", nullable: true),
                    xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_data_protection_keys", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_id = table.Column<long>(type: "bigint", nullable: false),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    linked_in_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_portfolio_public = table.Column<bool>(type: "boolean", nullable: false),
                    commit_emails = table.Column<List<string>>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "github_credentials",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    protected_token = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_github_credentials", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_github_credentials_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    image_urls = table.Column<List<string>>(type: "text[]", nullable: false),
                    linked_repository_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_projects", x => x.id);
                    table.ForeignKey(
                        name: "fk_projects_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    github_repo_id = table.Column<long>(type: "bigint", nullable: false),
                    owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    html_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    primary_language = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    stars = table.Column<int>(type: "integer", nullable: false),
                    forks = table.Column<int>(type: "integer", nullable: false),
                    is_fork = table.Column<bool>(type: "boolean", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    default_branch = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size_kb = table.Column<long>(type: "bigint", nullable: false),
                    last_activity = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_selected = table.Column<bool>(type: "boolean", nullable: false),
                    languages = table.Column<string>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_repositories", x => x.id);
                    table.ForeignKey(
                        name: "fk_repositories_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    overall_score = table.Column<int>(type: "integer", nullable: false),
                    activity_score = table.Column<int>(type: "integer", nullable: false),
                    structure_score = table.Column<int>(type: "integer", nullable: false),
                    quality_score = table.Column<int>(type: "integer", nullable: false),
                    head_commit_sha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    timeline = table.Column<string>(type: "jsonb", nullable: false),
                    largest_files = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_analyses_repositories_repository_id",
                        column: x => x.repository_id,
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analysis_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_runs_repositories_repository_id",
                        column: x => x.repository_id,
                        principalTable: "repositories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analysis_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<double>(type: "double precision", nullable: false),
                    included_in_score = table.Column<bool>(type: "boolean", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analysis_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_analysis_metrics_analyses_analysis_id",
                        column: x => x.analysis_id,
                        principalTable: "analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_strength = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_feedback", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedback_analyses_analysis_id",
                        column: x => x.analysis_id,
                        principalTable: "analyses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analyses_repository_id_scope_created_at",
                table: "analyses",
                columns: new[] { "repository_id", "scope", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_metrics_analysis_id_name",
                table: "analysis_metrics",
                columns: new[] { "analysis_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_repository_id_scope_status",
                table: "analysis_runs",
                columns: new[] { "repository_id", "scope", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_runs_user_id_requested_at",
                table: "analysis_runs",
                columns: new[] { "user_id", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_feedback_analysis_id",
                table: "feedback",
                column: "analysis_id");

            migrationBuilder.CreateIndex(
                name: "ix_projects_user_id_sort_order",
                table: "projects",
                columns: new[] { "user_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_repositories_user_id_github_repo_id",
                table: "repositories",
                columns: new[] { "user_id", "github_repo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_github_id",
                table: "users",
                column: "github_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_login",
                table: "users",
                column: "login");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_metrics");

            migrationBuilder.DropTable(
                name: "analysis_runs");

            migrationBuilder.DropTable(
                name: "data_protection_keys");

            migrationBuilder.DropTable(
                name: "feedback");

            migrationBuilder.DropTable(
                name: "github_credentials");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "analyses");

            migrationBuilder.DropTable(
                name: "repositories");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
