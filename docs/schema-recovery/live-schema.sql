-- SCHEMA-ONLY EXPORT (read-only, no user data), generated via information_schema/pg_catalog
-- Supabase Postgres, schema "public". Generated 2026-08-22.

-- ============================================================
-- TABLE: AiQuotaUsages
-- ============================================================
CREATE TABLE "AiQuotaUsages" (
  "Id" uuid NOT NULL,
  "UserId" integer NOT NULL,
  "UsageDate" timestamp with time zone NOT NULL,
  "TotalTokens" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "AiQuotaUsages" ADD PRIMARY KEY ("Id");
CREATE UNIQUE INDEX "IX_AiQuotaUsages_UserId_UsageDate" ON public."AiQuotaUsages" USING btree ("UserId", "UsageDate");

-- ============================================================
-- TABLE: Announcements
-- ============================================================
CREATE TABLE "Announcements" (
  "Id" integer NOT NULL,
  "ClassId" integer NOT NULL,
  "Title" text NOT NULL,
  "Content" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "Announcements" ADD PRIMARY KEY ("Id");
ALTER TABLE "Announcements" ADD CONSTRAINT "FK_Announcements_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Announcements_ClassId" ON public."Announcements" USING btree ("ClassId");

-- ============================================================
-- TABLE: AssignmentQuizDetails
-- ============================================================
CREATE TABLE "AssignmentQuizDetails" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "QuestionsJson" jsonb NOT NULL,
  "TimeLimitSeconds" integer,
  "ShuffleQuestions" boolean NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "AssignmentQuizDetails" ADD PRIMARY KEY ("Id");
ALTER TABLE "AssignmentQuizDetails" ADD CONSTRAINT "FK_AssignmentQuizDetails_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
CREATE UNIQUE INDEX "IX_AssignmentQuizDetails_AssignmentId" ON public."AssignmentQuizDetails" USING btree ("AssignmentId");

-- ============================================================
-- TABLE: AssignmentReportDetails
-- ============================================================
CREATE TABLE "AssignmentReportDetails" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "Instructions" text NOT NULL,
  "AllowedSubmissionTypesJson" jsonb NOT NULL,
  "AllowedFileExtensionsJson" jsonb NOT NULL,
  "MaxFileSizeMb" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "AssignmentReportDetails" ADD PRIMARY KEY ("Id");
ALTER TABLE "AssignmentReportDetails" ADD CONSTRAINT "FK_AssignmentReportDetails_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
CREATE UNIQUE INDEX "IX_AssignmentReportDetails_AssignmentId" ON public."AssignmentReportDetails" USING btree ("AssignmentId");

-- ============================================================
-- TABLE: AssignmentSimulationDetails
-- ============================================================
CREATE TABLE "AssignmentSimulationDetails" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "EnvironmentSource" character varying(40) NOT NULL,
  "BaseDiagramJson" jsonb NOT NULL,
  "AllowedComponentTypesJson" jsonb NOT NULL,
  "StudentInputMode" character varying(40) NOT NULL,
  "StarterCode" text,
  "AnswerKeyJson" jsonb NOT NULL,
  "AutoGradingEnabled" boolean NOT NULL,
  "AutoGradingWeight" double precision NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "AssignmentSimulationDetails" ADD PRIMARY KEY ("Id");
ALTER TABLE "AssignmentSimulationDetails" ADD CONSTRAINT "FK_AssignmentSimulationDetails_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
CREATE UNIQUE INDEX "IX_AssignmentSimulationDetails_AssignmentId" ON public."AssignmentSimulationDetails" USING btree ("AssignmentId");

-- ============================================================
-- TABLE: Assignments
-- ============================================================
CREATE TABLE "Assignments" (
  "Id" integer NOT NULL,
  "ClassId" integer NOT NULL,
  "Title" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "AllowResubmit" boolean NOT NULL DEFAULT false,
  "AssignmentType" character varying(40) NOT NULL DEFAULT 'text_report'::character varying,
  "CreatedById" integer,
  "Description" text NOT NULL DEFAULT ''::text,
  "DueDate" timestamp with time zone,
  "MaxScore" numeric NOT NULL DEFAULT 100.0,
  "ResubmitLimit" integer,
  "RubricId" integer,
  "Status" character varying(20) NOT NULL DEFAULT 'draft'::character varying
);
ALTER TABLE "Assignments" ADD PRIMARY KEY ("Id");
ALTER TABLE "Assignments" ADD CONSTRAINT "FK_Assignments_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Assignments_ClassId" ON public."Assignments" USING btree ("ClassId");

-- ============================================================
-- TABLE: AttendanceRecords
-- ============================================================
CREATE TABLE "AttendanceRecords" (
  "Id" integer NOT NULL,
  "ClassId" integer NOT NULL,
  "StudentId" integer NOT NULL,
  "AttendanceDate" date NOT NULL,
  "Status" character varying(20),
  "Note" character varying(500),
  "MarkedById" integer,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "ScheduleId" integer
);
ALTER TABLE "AttendanceRecords" ADD PRIMARY KEY ("Id");
ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Schedules_ScheduleId" FOREIGN KEY ("ScheduleId") REFERENCES "Schedules"("Id") ON DELETE NO ACTION;
ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Users_MarkedById" FOREIGN KEY ("MarkedById") REFERENCES "Users"("Id") ON DELETE RESTRICT;
ALTER TABLE "AttendanceRecords" ADD CONSTRAINT "FK_AttendanceRecords_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_AttendanceRecords_ScheduleId" ON public."AttendanceRecords" USING btree ("ScheduleId");
CREATE INDEX "IX_AttendanceRecords_MarkedById" ON public."AttendanceRecords" USING btree ("MarkedById");
CREATE INDEX "IX_AttendanceRecords_StudentId" ON public."AttendanceRecords" USING btree ("StudentId");
CREATE UNIQUE INDEX "IX_AttendanceRecords_ScheduleId_StudentId_Unique" ON public."AttendanceRecords" USING btree ("ScheduleId", "StudentId");

-- ============================================================
-- TABLE: Classes
-- ============================================================
CREATE TABLE "Classes" (
  "Id" integer NOT NULL,
  "CourseId" integer NOT NULL,
  "TeacherId" integer NOT NULL,
  "StartDate" timestamp with time zone NOT NULL,
  "EndDate" timestamp with time zone NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "ClassCode" text NOT NULL DEFAULT ''::text,
  "SchoolId" integer NOT NULL DEFAULT 0,
  "GradeLevelId" integer NOT NULL DEFAULT 0
);
ALTER TABLE "Classes" ADD PRIMARY KEY ("Id");
ALTER TABLE "Classes" ADD CONSTRAINT "FK_Classes_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses"("Id") ON DELETE RESTRICT;
ALTER TABLE "Classes" ADD CONSTRAINT "FK_Classes_GradeLevels_GradeLevelId" FOREIGN KEY ("GradeLevelId") REFERENCES "GradeLevels"("Id") ON DELETE RESTRICT;
ALTER TABLE "Classes" ADD CONSTRAINT "FK_Classes_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES "Schools"("Id") ON DELETE RESTRICT;
ALTER TABLE "Classes" ADD CONSTRAINT "FK_Classes_Users_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_Classes_GradeLevelId" ON public."Classes" USING btree ("GradeLevelId");
CREATE INDEX "IX_Classes_CourseId" ON public."Classes" USING btree ("CourseId");
CREATE INDEX "IX_Classes_TeacherId" ON public."Classes" USING btree ("TeacherId");
CREATE INDEX "IX_Classes_SchoolId" ON public."Classes" USING btree ("SchoolId");

-- ============================================================
-- TABLE: ComponentDefinitions
-- ============================================================
CREATE TABLE "ComponentDefinitions" (
  "Id" uuid NOT NULL,
  "CanonicalKey" character varying(120) NOT NULL,
  "Name" character varying(200) NOT NULL,
  "Category" text,
  "Status" character varying(30) NOT NULL,
  "SimulationComponentType" character varying(80),
  "PinsJson" jsonb NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "ComponentDefinitions" ADD PRIMARY KEY ("Id");
CREATE UNIQUE INDEX "IX_ComponentDefinitions_CanonicalKey" ON public."ComponentDefinitions" USING btree ("CanonicalKey");

-- ============================================================
-- TABLE: ComponentGlueRegistry
-- ============================================================
CREATE TABLE "ComponentGlueRegistry" (
  "ComponentType" character varying(80) NOT NULL,
  "Label" character varying(120) NOT NULL,
  "Supported" boolean NOT NULL,
  "PinRequirementsJson" jsonb NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "ComponentGlueRegistry" ADD PRIMARY KEY ("ComponentType");

-- ============================================================
-- TABLE: ComponentSources
-- ============================================================
CREATE TABLE "ComponentSources" (
  "Id" uuid NOT NULL,
  "ComponentId" uuid NOT NULL,
  "Provider" character varying(60) NOT NULL,
  "ExternalId" character varying(300) NOT NULL,
  "SourceUrl" text NOT NULL,
  "License" text,
  "LicenseStatus" character varying(30) NOT NULL,
  "ExternalVersion" text,
  "Checksum" text,
  "AssetsJson" jsonb NOT NULL,
  "ImportedAt" timestamp with time zone NOT NULL,
  "LastSyncedAt" timestamp with time zone
);
ALTER TABLE "ComponentSources" ADD PRIMARY KEY ("Id");
ALTER TABLE "ComponentSources" ADD CONSTRAINT "FK_ComponentSources_ComponentDefinitions_ComponentId" FOREIGN KEY ("ComponentId") REFERENCES "ComponentDefinitions"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_ComponentSources_ComponentId" ON public."ComponentSources" USING btree ("ComponentId");
CREATE UNIQUE INDEX "IX_ComponentSources_Provider_ExternalId" ON public."ComponentSources" USING btree ("Provider", "ExternalId");

-- ============================================================
-- TABLE: Courses
-- ============================================================
CREATE TABLE "Courses" (
  "Id" integer NOT NULL,
  "Title" text NOT NULL,
  "Description" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "SchoolId" integer,
  "DisplayOrder" integer NOT NULL DEFAULT 0,
  "EstimatedHours" integer NOT NULL DEFAULT 0,
  "IsRequired" boolean NOT NULL DEFAULT false,
  "Status" text NOT NULL DEFAULT ''::text,
  "SubjectArea" text NOT NULL DEFAULT ''::text,
  "SyllabusId" integer,
  "IsActive" boolean NOT NULL DEFAULT false
);
ALTER TABLE "Courses" ADD PRIMARY KEY ("Id");
ALTER TABLE "Courses" ADD CONSTRAINT "FK_Courses_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES "Schools"("Id") ON DELETE NO ACTION;
ALTER TABLE "Courses" ADD CONSTRAINT "FK_Courses_Syllabuses_SyllabusId" FOREIGN KEY ("SyllabusId") REFERENCES "Syllabuses"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Courses_SyllabusId" ON public."Courses" USING btree ("SyllabusId");
CREATE INDEX "IX_Courses_SchoolId" ON public."Courses" USING btree ("SchoolId");

-- ============================================================
-- TABLE: Enrollments
-- ============================================================
CREATE TABLE "Enrollments" (
  "Id" integer NOT NULL,
  "ClassId" integer NOT NULL,
  "StudentId" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "EnrolledAt" timestamp with time zone NOT NULL DEFAULT '-infinity'::timestamp with time zone
);
ALTER TABLE "Enrollments" ADD PRIMARY KEY ("Id");
ALTER TABLE "Enrollments" ADD CONSTRAINT "FK_Enrollments_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
ALTER TABLE "Enrollments" ADD CONSTRAINT "FK_Enrollments_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Enrollments_ClassId" ON public."Enrollments" USING btree ("ClassId");
CREATE INDEX "IX_Enrollments_StudentId" ON public."Enrollments" USING btree ("StudentId");

-- ============================================================
-- TABLE: FileEntity
-- ============================================================
CREATE TABLE "FileEntity" (
  "Id" integer NOT NULL,
  "Url" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "FileEntity" ADD PRIMARY KEY ("Id");

-- ============================================================
-- TABLE: GradeLevels
-- ============================================================
CREATE TABLE "GradeLevels" (
  "Id" integer NOT NULL,
  "Name" text NOT NULL,
  "Code" text NOT NULL,
  "DisplayOrder" integer NOT NULL,
  "Description" text NOT NULL,
  "Level" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "GradeLevels" ADD PRIMARY KEY ("Id");

-- ============================================================
-- TABLE: LabClassAssignments
-- ============================================================
CREATE TABLE "LabClassAssignments" (
  "Id" uuid NOT NULL,
  "LabId" uuid NOT NULL,
  "ClassId" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "LabClassAssignments" ADD PRIMARY KEY ("Id");
ALTER TABLE "LabClassAssignments" ADD CONSTRAINT "FK_LabClassAssignments_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
ALTER TABLE "LabClassAssignments" ADD CONSTRAINT "FK_LabClassAssignments_Labs_LabId" FOREIGN KEY ("LabId") REFERENCES "Labs"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_LabClassAssignments_ClassId" ON public."LabClassAssignments" USING btree ("ClassId");
CREATE UNIQUE INDEX "IX_LabClassAssignments_LabId_ClassId" ON public."LabClassAssignments" USING btree ("LabId", "ClassId");

-- ============================================================
-- TABLE: LabProgresses
-- ============================================================
CREATE TABLE "LabProgresses" (
  "Id" uuid NOT NULL,
  "LabId" uuid NOT NULL,
  "StudentId" integer NOT NULL,
  "StartedAt" timestamp with time zone NOT NULL,
  "LastOpenedAt" timestamp with time zone NOT NULL,
  "OpenCount" integer NOT NULL,
  "CompletedAt" timestamp with time zone,
  "DurationSeconds" integer
);
ALTER TABLE "LabProgresses" ADD PRIMARY KEY ("Id");
ALTER TABLE "LabProgresses" ADD CONSTRAINT "FK_LabProgresses_Labs_LabId" FOREIGN KEY ("LabId") REFERENCES "Labs"("Id") ON DELETE CASCADE;
ALTER TABLE "LabProgresses" ADD CONSTRAINT "FK_LabProgresses_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE UNIQUE INDEX "IX_LabProgresses_LabId_StudentId" ON public."LabProgresses" USING btree ("LabId", "StudentId");
CREATE INDEX "IX_LabProgresses_StudentId" ON public."LabProgresses" USING btree ("StudentId");

-- ============================================================
-- TABLE: Labs
-- ============================================================
CREATE TABLE "Labs" (
  "Id" uuid NOT NULL,
  "Title" character varying(200) NOT NULL,
  "Description" text NOT NULL,
  "Category" character varying(40) NOT NULL,
  "ThumbnailUrl" character varying(2048) NOT NULL,
  "WokwiProjectId" character varying(80),
  "WokwiProjectUrl" character varying(2048),
  "CreatedById" integer NOT NULL,
  "LinkedAssignmentId" integer,
  "Status" character varying(20) NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "AllowedComponentTypesJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
  "BoardType" character varying(40) NOT NULL DEFAULT 'arduino_uno'::character varying,
  "CircuitConfigJson" jsonb NOT NULL DEFAULT '{}'::jsonb,
  "SimulationMode" character varying(40) NOT NULL DEFAULT 'wokwi_iframe'::character varying,
  "StarterCode" text
);
ALTER TABLE "Labs" ADD PRIMARY KEY ("Id");
ALTER TABLE "Labs" ADD CONSTRAINT "FK_Labs_Assignments_LinkedAssignmentId" FOREIGN KEY ("LinkedAssignmentId") REFERENCES "Assignments"("Id") ON DELETE SET NULL;
ALTER TABLE "Labs" ADD CONSTRAINT "FK_Labs_Users_CreatedById" FOREIGN KEY ("CreatedById") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_Labs_Category" ON public."Labs" USING btree ("Category");
CREATE INDEX "IX_Labs_CreatedById" ON public."Labs" USING btree ("CreatedById");
CREATE INDEX "IX_Labs_LinkedAssignmentId" ON public."Labs" USING btree ("LinkedAssignmentId");
CREATE INDEX "IX_Labs_Status" ON public."Labs" USING btree ("Status");

-- ============================================================
-- TABLE: Lessons
-- ============================================================
CREATE TABLE "Lessons" (
  "Id" integer NOT NULL,
  "ModuleId" integer NOT NULL,
  "Title" text NOT NULL,
  "Content" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "DisplayOrder" integer NOT NULL DEFAULT 0,
  "EstimatedMinutes" integer NOT NULL DEFAULT 0,
  "HasVirtualLab" boolean NOT NULL DEFAULT false,
  "Input" text NOT NULL DEFAULT ''::text,
  "LabId" uuid,
  "LessonType" text NOT NULL DEFAULT ''::text,
  "Output" text NOT NULL DEFAULT ''::text
);
ALTER TABLE "Lessons" ADD PRIMARY KEY ("Id");
ALTER TABLE "Lessons" ADD CONSTRAINT "FK_Lessons_Modules_ModuleId" FOREIGN KEY ("ModuleId") REFERENCES "Modules"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Lessons_ModuleId" ON public."Lessons" USING btree ("ModuleId");

-- ============================================================
-- TABLE: LoginHistories
-- ============================================================
CREATE TABLE "LoginHistories" (
  "Id" integer NOT NULL,
  "UserId" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "DeviceName" text NOT NULL DEFAULT ''::text,
  "IpAddress" text NOT NULL DEFAULT ''::text
);
ALTER TABLE "LoginHistories" ADD PRIMARY KEY ("Id");
ALTER TABLE "LoginHistories" ADD CONSTRAINT "FK_LoginHistories_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_LoginHistories_UserId" ON public."LoginHistories" USING btree ("UserId");

-- ============================================================
-- TABLE: Metrics
-- ============================================================
CREATE TABLE "Metrics" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "Criteria" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "Metrics" ADD PRIMARY KEY ("Id");
ALTER TABLE "Metrics" ADD CONSTRAINT "FK_Metrics_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Metrics_AssignmentId" ON public."Metrics" USING btree ("AssignmentId");

-- ============================================================
-- TABLE: Modules
-- ============================================================
CREATE TABLE "Modules" (
  "Id" integer NOT NULL,
  "CourseId" integer NOT NULL,
  "Title" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "Description" text NOT NULL DEFAULT ''::text,
  "DisplayOrder" integer NOT NULL DEFAULT 0,
  "EstimatedMinutes" integer NOT NULL DEFAULT 0,
  "Input" text NOT NULL DEFAULT ''::text,
  "Output" text NOT NULL DEFAULT ''::text
);
ALTER TABLE "Modules" ADD PRIMARY KEY ("Id");
ALTER TABLE "Modules" ADD CONSTRAINT "FK_Modules_Courses_CourseId" FOREIGN KEY ("CourseId") REFERENCES "Courses"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Modules_CourseId" ON public."Modules" USING btree ("CourseId");

-- ============================================================
-- TABLE: Notifications
-- ============================================================
CREATE TABLE "Notifications" (
  "Id" integer NOT NULL,
  "UserId" integer NOT NULL,
  "Content" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "IsRead" boolean NOT NULL DEFAULT false,
  "Title" text NOT NULL DEFAULT ''::text,
  "Type" text
);
ALTER TABLE "Notifications" ADD PRIMARY KEY ("Id");
ALTER TABLE "Notifications" ADD CONSTRAINT "FK_Notifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Notifications_UserId" ON public."Notifications" USING btree ("UserId");

-- ============================================================
-- TABLE: PaymentPackages
-- ============================================================
CREATE TABLE "PaymentPackages" (
  "Id" integer NOT NULL,
  "Name" character varying(200) NOT NULL,
  "Description" character varying(1000),
  "Price" numeric NOT NULL,
  "Currency" character varying(10) NOT NULL DEFAULT 'VND'::character varying,
  "TokenAmount" integer NOT NULL,
  "IsActive" boolean NOT NULL,
  "IsFeatured" boolean NOT NULL,
  "Features" jsonb,
  "DisplayOrder" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "ExpiresAt" timestamp with time zone NOT NULL DEFAULT '-infinity'::timestamp with time zone,
  "StudentLimit" integer NOT NULL DEFAULT 0,
  "DurationMonths" integer NOT NULL DEFAULT 0
);
ALTER TABLE "PaymentPackages" ADD PRIMARY KEY ("Id");

-- ============================================================
-- TABLE: Payments
-- ============================================================
CREATE TABLE "Payments" (
  "Id" integer NOT NULL,
  "TransactionId" character varying(100) NOT NULL,
  "PackageId" integer NOT NULL,
  "SchoolId" integer,
  "UserId" integer,
  "TokenAmount" integer NOT NULL,
  "Amount" numeric NOT NULL,
  "Currency" character varying(10) NOT NULL DEFAULT 'VND'::character varying,
  "Status" integer NOT NULL,
  "Method" integer NOT NULL,
  "GatewayTransactionId" character varying(100),
  "PaymentLinkId" character varying(100),
  "CheckoutUrl" character varying(500),
  "PaidAt" timestamp with time zone,
  "ExpiresAt" timestamp with time zone,
  "CanceledAt" timestamp with time zone,
  "CancellationReason" text,
  "Metadata" jsonb,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "OrderCode" bigint
);
ALTER TABLE "Payments" ADD PRIMARY KEY ("Id");
ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_PaymentPackages_PackageId" FOREIGN KEY ("PackageId") REFERENCES "PaymentPackages"("Id") ON DELETE RESTRICT;
ALTER TABLE "Payments" ADD CONSTRAINT "FK_Payments_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES "Schools"("Id") ON DELETE SET NULL;
CREATE INDEX "IX_Payments_PackageId" ON public."Payments" USING btree ("PackageId");
CREATE INDEX "IX_Payments_PaymentLinkId" ON public."Payments" USING btree ("PaymentLinkId");
CREATE INDEX "IX_Payments_SchoolId" ON public."Payments" USING btree ("SchoolId");
CREATE UNIQUE INDEX "IX_Payments_TransactionId" ON public."Payments" USING btree ("TransactionId");

-- ============================================================
-- TABLE: RefreshTokens
-- ============================================================
CREATE TABLE "RefreshTokens" (
  "Id" integer NOT NULL,
  "UserId" integer NOT NULL,
  "Token" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "ExpiresAt" timestamp with time zone NOT NULL DEFAULT '-infinity'::timestamp with time zone
);
ALTER TABLE "RefreshTokens" ADD PRIMARY KEY ("Id");
ALTER TABLE "RefreshTokens" ADD CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_RefreshTokens_UserId" ON public."RefreshTokens" USING btree ("UserId");

-- ============================================================
-- TABLE: ResubmitRequests
-- ============================================================
CREATE TABLE "ResubmitRequests" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "StudentId" integer NOT NULL,
  "Reason" character varying(1000),
  "Status" text NOT NULL,
  "GrantedExtraAttempts" integer,
  "GrantedNewDueDate" timestamp with time zone,
  "ReviewNote" character varying(1000),
  "ReviewedById" integer,
  "ReviewedAt" timestamp with time zone,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "ResubmitRequests" ADD PRIMARY KEY ("Id");
ALTER TABLE "ResubmitRequests" ADD CONSTRAINT "FK_ResubmitRequests_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
ALTER TABLE "ResubmitRequests" ADD CONSTRAINT "FK_ResubmitRequests_Users_ReviewedById" FOREIGN KEY ("ReviewedById") REFERENCES "Users"("Id") ON DELETE RESTRICT;
ALTER TABLE "ResubmitRequests" ADD CONSTRAINT "FK_ResubmitRequests_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_ResubmitRequests_AssignmentId" ON public."ResubmitRequests" USING btree ("AssignmentId");
CREATE INDEX "IX_ResubmitRequests_ReviewedById" ON public."ResubmitRequests" USING btree ("ReviewedById");
CREATE INDEX "IX_ResubmitRequests_StudentId" ON public."ResubmitRequests" USING btree ("StudentId");

-- ============================================================
-- TABLE: Roles
-- ============================================================
CREATE TABLE "Roles" (
  "Id" integer NOT NULL,
  "Name" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "Roles" ADD PRIMARY KEY ("Id");

-- ============================================================
-- TABLE: Rubrics
-- ============================================================
CREATE TABLE "Rubrics" (
  "Id" integer NOT NULL,
  "Criteria" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "AssignmentId" integer NOT NULL DEFAULT 0,
  "MaxScore" integer NOT NULL DEFAULT 0
);
ALTER TABLE "Rubrics" ADD PRIMARY KEY ("Id");
ALTER TABLE "Rubrics" ADD CONSTRAINT "FK_Rubrics_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_Rubrics_AssignmentId" ON public."Rubrics" USING btree ("AssignmentId");

-- ============================================================
-- TABLE: Schedules
-- ============================================================
CREATE TABLE "Schedules" (
  "Id" integer NOT NULL,
  "ClassId" integer NOT NULL,
  "StartTime" timestamp with time zone NOT NULL,
  "EndTime" timestamp with time zone NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "LessonId" integer
);
ALTER TABLE "Schedules" ADD PRIMARY KEY ("Id");
ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Classes_ClassId" FOREIGN KEY ("ClassId") REFERENCES "Classes"("Id") ON DELETE CASCADE;
ALTER TABLE "Schedules" ADD CONSTRAINT "FK_Schedules_Lessons_LessonId" FOREIGN KEY ("LessonId") REFERENCES "Lessons"("Id") ON DELETE RESTRICT;
CREATE UNIQUE INDEX "IX_Schedules_LessonId" ON public."Schedules" USING btree ("LessonId") WHERE ("LessonId" IS NOT NULL);
CREATE INDEX "IX_Schedules_ClassId" ON public."Schedules" USING btree ("ClassId");

-- ============================================================
-- TABLE: Schools
-- ============================================================
CREATE TABLE "Schools" (
  "Id" integer NOT NULL,
  "Name" text NOT NULL,
  "Address" text NOT NULL,
  "RepresentativeEmail" text NOT NULL,
  "RepresentativeName" text NOT NULL,
  "ProofOfActivity" text,
  "Status" integer NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "StudentScale" text,
  "RepresentativePosition" text,
  "Website" text,
  "Notes" text,
  "AttachmentFileName" text,
  "AttachmentUrl" text,
  "OriginalAttachmentFileName" text,
  "RejectionReason" text
);
ALTER TABLE "Schools" ADD PRIMARY KEY ("Id");

-- ============================================================
-- TABLE: SubmissionComments
-- ============================================================
CREATE TABLE "SubmissionComments" (
  "Id" integer NOT NULL,
  "SubmissionId" integer NOT NULL,
  "AuthorId" integer NOT NULL,
  "Body" character varying(2000) NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "SubmissionComments" ADD PRIMARY KEY ("Id");
ALTER TABLE "SubmissionComments" ADD CONSTRAINT "FK_SubmissionComments_Submissions_SubmissionId" FOREIGN KEY ("SubmissionId") REFERENCES "Submissions"("Id") ON DELETE CASCADE;
ALTER TABLE "SubmissionComments" ADD CONSTRAINT "FK_SubmissionComments_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_SubmissionComments_AuthorId" ON public."SubmissionComments" USING btree ("AuthorId");
CREATE INDEX "IX_SubmissionComments_SubmissionId" ON public."SubmissionComments" USING btree ("SubmissionId");

-- ============================================================
-- TABLE: Submissions
-- ============================================================
CREATE TABLE "Submissions" (
  "Id" integer NOT NULL,
  "AssignmentId" integer NOT NULL,
  "FileId" integer,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "Feedback" character varying(1000),
  "GradedAt" timestamp with time zone,
  "GradedById" integer,
  "Score" numeric,
  "StudentId" integer,
  "AttemptNumber" integer NOT NULL DEFAULT 1,
  "AutoGradeResultJson" jsonb,
  "AutoScore" numeric,
  "ContentJson" jsonb NOT NULL DEFAULT '{}'::jsonb,
  "FinalScore" numeric,
  "Status" character varying(20) NOT NULL DEFAULT 'submitted'::character varying,
  "SubmittedAt" timestamp with time zone
);
ALTER TABLE "Submissions" ADD PRIMARY KEY ("Id");
ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_Assignments_AssignmentId" FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE CASCADE;
ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_Files_FileId" FOREIGN KEY ("FileId") REFERENCES "FileEntity"("Id") ON DELETE CASCADE;
ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_Users_GradedById" FOREIGN KEY ("GradedById") REFERENCES "Users"("Id") ON DELETE RESTRICT;
ALTER TABLE "Submissions" ADD CONSTRAINT "FK_Submissions_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_Submissions_GradedById" ON public."Submissions" USING btree ("GradedById");
CREATE INDEX "IX_Submissions_StudentId" ON public."Submissions" USING btree ("StudentId");
CREATE INDEX "IX_Submissions_FileId" ON public."Submissions" USING btree ("FileId");
CREATE UNIQUE INDEX "IX_Submissions_AssignmentId_StudentId_AttemptNumber" ON public."Submissions" USING btree ("AssignmentId", "StudentId", "AttemptNumber");

-- ============================================================
-- TABLE: Syllabuses
-- ============================================================
CREATE TABLE "Syllabuses" (
  "Id" integer NOT NULL,
  "Title" text NOT NULL,
  "Description" text NOT NULL,
  "ThumbnailUrl" text,
  "GradeLevelId" integer,
  "SubjectArea" text NOT NULL,
  "Status" text NOT NULL,
  "DisplayOrder" integer NOT NULL,
  "EstimatedHours" integer NOT NULL,
  "IsRequired" boolean NOT NULL,
  "IsSystemOwned" boolean NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "Syllabuses" ADD PRIMARY KEY ("Id");
ALTER TABLE "Syllabuses" ADD CONSTRAINT "FK_Syllabuses_GradeLevels_GradeLevelId" FOREIGN KEY ("GradeLevelId") REFERENCES "GradeLevels"("Id") ON DELETE SET NULL;
CREATE INDEX "IX_Syllabuses_GradeLevelId" ON public."Syllabuses" USING btree ("GradeLevelId");

-- ============================================================
-- TABLE: TokenAccounts
-- ============================================================
CREATE TABLE "TokenAccounts" (
  "Id" integer NOT NULL,
  "SchoolId" integer NOT NULL,
  "TotalTokensPurchased" integer NOT NULL,
  "TokensRemaining" integer NOT NULL,
  "TokensUsed" integer NOT NULL,
  "ExpiresAt" timestamp with time zone,
  "LastPurchaseAt" timestamp with time zone,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "TokenAccounts" ADD PRIMARY KEY ("Id");
ALTER TABLE "TokenAccounts" ADD CONSTRAINT "FK_TokenAccounts_Schools_SchoolId" FOREIGN KEY ("SchoolId") REFERENCES "Schools"("Id") ON DELETE CASCADE;
CREATE UNIQUE INDEX "IX_TokenAccounts_SchoolId" ON public."TokenAccounts" USING btree ("SchoolId");

-- ============================================================
-- TABLE: TokenAllocations
-- ============================================================
CREATE TABLE "TokenAllocations" (
  "Id" integer NOT NULL,
  "AccountId" integer NOT NULL,
  "UserId" integer NOT NULL,
  "AllocatedTokens" integer NOT NULL,
  "UsedTokens" integer NOT NULL,
  "ExpiresAt" timestamp with time zone,
  "Notes" character varying(500),
  "AllocatedByUserId" integer NOT NULL,
  "IsActive" boolean NOT NULL,
  "RevokedAt" timestamp with time zone,
  "RevocationReason" character varying(500),
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone
);
ALTER TABLE "TokenAllocations" ADD PRIMARY KEY ("Id");
ALTER TABLE "TokenAllocations" ADD CONSTRAINT "FK_TokenAllocations_TokenAccounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "TokenAccounts"("Id") ON DELETE CASCADE;
ALTER TABLE "TokenAllocations" ADD CONSTRAINT "FK_TokenAllocations_Users_AllocatedByUserId" FOREIGN KEY ("AllocatedByUserId") REFERENCES "Users"("Id") ON DELETE RESTRICT;
ALTER TABLE "TokenAllocations" ADD CONSTRAINT "FK_TokenAllocations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_TokenAllocations_AccountId_UserId" ON public."TokenAllocations" USING btree ("AccountId", "UserId");
CREATE INDEX "IX_TokenAllocations_AllocatedByUserId" ON public."TokenAllocations" USING btree ("AllocatedByUserId");
CREATE INDEX "IX_TokenAllocations_UserId" ON public."TokenAllocations" USING btree ("UserId");

-- ============================================================
-- TABLE: TokenTransactions
-- ============================================================
CREATE TABLE "TokenTransactions" (
  "Id" integer NOT NULL,
  "PaymentId" integer,
  "AccountId" integer NOT NULL,
  "Type" integer NOT NULL,
  "Quantity" integer NOT NULL,
  "BalanceAfter" integer NOT NULL,
  "Description" character varying(500),
  "ReferenceId" character varying(100),
  "Metadata" jsonb,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL
);
ALTER TABLE "TokenTransactions" ADD PRIMARY KEY ("Id");
ALTER TABLE "TokenTransactions" ADD CONSTRAINT "FK_TokenTransactions_Payments_PaymentId" FOREIGN KEY ("PaymentId") REFERENCES "Payments"("Id") ON DELETE SET NULL;
ALTER TABLE "TokenTransactions" ADD CONSTRAINT "FK_TokenTransactions_TokenAccounts_AccountId" FOREIGN KEY ("AccountId") REFERENCES "TokenAccounts"("Id") ON DELETE CASCADE;
CREATE INDEX "IX_TokenTransactions_AccountId" ON public."TokenTransactions" USING btree ("AccountId");
CREATE INDEX "IX_TokenTransactions_PaymentId" ON public."TokenTransactions" USING btree ("PaymentId");

-- ============================================================
-- TABLE: Users
-- ============================================================
CREATE TABLE "Users" (
  "Id" integer NOT NULL,
  "PasswordHash" text,
  "Email" text NOT NULL,
  "IsActive" boolean NOT NULL,
  "RoleId" integer NOT NULL,
  "IsEmailVerified" boolean NOT NULL,
  "VerificationToken" text,
  "VerificationTokenExpires" timestamp with time zone,
  "ResetToken" text,
  "ResetTokenExpires" timestamp with time zone,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "Address" text,
  "Avatar" text,
  "DateOfBirth" date,
  "FullName" text NOT NULL DEFAULT ''::text,
  "Gender" text,
  "Phone" text,
  "SchoolId" integer
);
ALTER TABLE "Users" ADD PRIMARY KEY ("Id");
ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "Roles"("Id") ON DELETE RESTRICT;
CREATE INDEX "IX_Users_RoleId" ON public."Users" USING btree ("RoleId");

-- ============================================================
-- TABLE: VirtualLabProjects
-- ============================================================
CREATE TABLE "VirtualLabProjects" (
  "Id" uuid NOT NULL,
  "UserId" integer,
  "Name" text NOT NULL,
  "Board" text NOT NULL,
  "Language" text NOT NULL,
  "CodeContent" text NOT NULL,
  "DiagramJson" text NOT NULL,
  "LibrariesJson" text NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "Status" character varying(20) NOT NULL DEFAULT 'stopped'::character varying,
  "SimulationEventsJson" jsonb NOT NULL DEFAULT '[]'::jsonb,
  "LabId" uuid
);
ALTER TABLE "VirtualLabProjects" ADD PRIMARY KEY ("Id");
ALTER TABLE "VirtualLabProjects" ADD CONSTRAINT "FK_VirtualLabProjects_Labs_LabId" FOREIGN KEY ("LabId") REFERENCES "Labs"("Id") ON DELETE SET NULL;
CREATE INDEX "IX_VirtualLabProjects_LabId" ON public."VirtualLabProjects" USING btree ("LabId");

-- ============================================================
-- SEQUENCES (0)
-- ============================================================
