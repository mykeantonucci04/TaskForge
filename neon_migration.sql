CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE public.tenants (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Slug" character varying(50) NOT NULL,
    "Plan" integer NOT NULL,
    "Status" integer NOT NULL,
    "SchemaName" character varying(63) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastModifiedAtUtc" timestamp with time zone,
    "LastModifiedBy" text,
    CONSTRAINT "PK_tenants" PRIMARY KEY ("Id")
);

CREATE TABLE public.users (
    "Id" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastModifiedAtUtc" timestamp with time zone,
    "LastModifiedBy" text,
    CONSTRAINT "PK_users" PRIMARY KEY ("Id")
);

CREATE TABLE public.refresh_tokens (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" character varying(256) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "CreatedByIp" character varying(50),
    "RevokedAtUtc" timestamp with time zone,
    "RevokedByIp" character varying(50),
    "ReplacedByToken" character varying(256),
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_refresh_tokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_refresh_tokens_users_UserId" FOREIGN KEY ("UserId") REFERENCES public.users ("Id") ON DELETE CASCADE
);

CREATE TABLE public.tenant_invitations (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "Role" integer NOT NULL,
    "Token" character varying(128) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "InvitedById" uuid NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastModifiedAtUtc" timestamp with time zone,
    "LastModifiedBy" text,
    CONSTRAINT "PK_tenant_invitations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_tenant_invitations_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES public.tenants ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_tenant_invitations_users_InvitedById" FOREIGN KEY ("InvitedById") REFERENCES public.users ("Id") ON DELETE RESTRICT
);

CREATE TABLE public.tenant_memberships (
    "Id" uuid NOT NULL,
    "TenantId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "Status" integer NOT NULL,
    "JoinedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "CreatedBy" text,
    "LastModifiedAtUtc" timestamp with time zone,
    "LastModifiedBy" text,
    CONSTRAINT "PK_tenant_memberships" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_tenant_memberships_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES public.tenants ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_tenant_memberships_users_UserId" FOREIGN KEY ("UserId") REFERENCES public.users ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_refresh_tokens_Token" ON public.refresh_tokens ("Token");

CREATE INDEX "IX_refresh_tokens_UserId" ON public.refresh_tokens ("UserId");

CREATE INDEX "IX_tenant_invitations_InvitedById" ON public.tenant_invitations ("InvitedById");

CREATE INDEX "IX_tenant_invitations_TenantId" ON public.tenant_invitations ("TenantId");

CREATE UNIQUE INDEX "IX_tenant_invitations_Token" ON public.tenant_invitations ("Token");

CREATE UNIQUE INDEX "IX_tenant_memberships_TenantId_UserId" ON public.tenant_memberships ("TenantId", "UserId");

CREATE INDEX "IX_tenant_memberships_UserId" ON public.tenant_memberships ("UserId");

CREATE UNIQUE INDEX "IX_tenants_Slug" ON public.tenants ("Slug");

CREATE UNIQUE INDEX "IX_users_Email" ON public.users ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260901133845_InitialPublicSchema', '9.0.2');

COMMIT;

