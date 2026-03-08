CREATE DATABASE  IF NOT EXISTS `assignment_management_db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `assignment_management_db`;
-- MySQL dump 10.13  Distrib 8.0.44, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: assignment_management_db
-- ------------------------------------------------------
-- Server version	8.0.44

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `ai_schedules`
--

DROP TABLE IF EXISTS `ai_schedules`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ai_schedules` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `assignment_id` int DEFAULT NULL,
  `slot_date` date NOT NULL,
  `start_time` time NOT NULL,
  `end_time` time NOT NULL,
  `notes` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `assignment_id` (`assignment_id`),
  KEY `idx_ai_schedules_user_date` (`user_id`,`slot_date`),
  CONSTRAINT `ai_schedules_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE,
  CONSTRAINT `ai_schedules_ibfk_2` FOREIGN KEY (`assignment_id`) REFERENCES `assignments` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=21 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ai_schedules`
--

LOCK TABLES `ai_schedules` WRITE;
/*!40000 ALTER TABLE `ai_schedules` DISABLE KEYS */;
INSERT INTO `ai_schedules` VALUES (17,1,5,'2026-03-08','08:00:00','09:00:00','yh','2026-03-08 23:00:37'),(18,1,5,'2026-03-09','08:00:00','09:00:00','yh','2026-03-08 23:00:37'),(19,1,4,'2026-03-08','08:00:00','09:00:00','ok','2026-03-08 23:00:37'),(20,1,4,'2026-03-09','08:00:00','09:00:00','ok','2026-03-08 23:00:37');
/*!40000 ALTER TABLE `ai_schedules` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `assignment_roadmap_steps`
--

DROP TABLE IF EXISTS `assignment_roadmap_steps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `assignment_roadmap_steps` (
  `id` int NOT NULL AUTO_INCREMENT,
  `assignment_id` int NOT NULL,
  `step_order` int NOT NULL,
  `step_title` varchar(500) NOT NULL,
  `step_detail` text,
  `suggested_date` date DEFAULT NULL,
  `suggested_time` time DEFAULT NULL,
  `suggested_end_time` time DEFAULT NULL,
  `completed` tinyint(1) DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `assignment_id` (`assignment_id`),
  CONSTRAINT `assignment_roadmap_steps_ibfk_1` FOREIGN KEY (`assignment_id`) REFERENCES `assignments` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=158 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `assignment_roadmap_steps`
--

LOCK TABLES `assignment_roadmap_steps` WRITE;
/*!40000 ALTER TABLE `assignment_roadmap_steps` DISABLE KEYS */;
INSERT INTO `assignment_roadmap_steps` VALUES (139,5,1,'Day 1: Read the assignment and list all requirements','Open the assignment file or PDF. Read every question and instruction. Note word limits, due dates, and marking criteria. List all deliverables on a page.','2026-03-08','08:00:00','09:00:00',0,'2026-03-08 22:24:43'),(140,5,2,'Day 2: Research and gather materials / sources','Search books, articles, or the web for 3–5 reliable sources. Save links and key points. Note citation format (e.g. APA).','2026-03-09','08:00:00','09:00:00',0,'2026-03-08 22:24:43'),(141,5,3,'Day 3: Outline and plan structure','Create headings and sub-points for your answer. Decide what goes in intro, body, and conclusion. Allocate word count per section.','2026-03-10','08:00:00','09:00:00',0,'2026-03-08 22:24:43'),(142,5,4,'Day 4: Draft first version or first section','Write the first section or first draft without worrying about perfection. Follow your outline. Use clear sentences.','2026-03-11','08:00:00','09:00:00',0,'2026-03-08 22:24:43'),(143,5,5,'Day 1: Draft remaining sections','Complete the remaining sections or paragraphs. Keep tone and style consistent. Support claims with evidence.','2026-03-08','08:00:00','09:00:00',0,'2026-03-08 22:24:43'),(151,4,1,'Day 1: ok: Read the assignment and list all requirements','Open the assignment file or PDF. Read every question and instruction. Note word limits, due dates, and marking criteria. List all deliverables on a page.','2026-03-08','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(152,4,2,'Day 2: ok: Research and gather materials / sources','Search books, articles, or the web for 3–5 reliable sources. Save links and key points. Note citation format (e.g. APA).','2026-03-09','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(153,4,3,'Day 3: ok: Outline and plan structure','Create headings and sub-points for your answer. Decide what goes in intro, body, and conclusion. Allocate word count per section.','2026-03-10','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(154,4,4,'Day 4: ok: Draft first version or first section','Write the first section or first draft without worrying about perfection. Follow your outline. Use clear sentences.','2026-03-11','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(155,4,5,'Day 5: ok: Draft remaining sections','Complete the remaining sections or paragraphs. Keep tone and style consistent. Support claims with evidence.','2026-03-12','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(156,4,6,'Day 6: ok: Review and revise content','Re-read the full draft. Improve clarity, flow, and logic. Cut or add where needed. Check it answers the question.','2026-03-13','08:00:00','09:00:00',0,'2026-03-08 22:30:46'),(157,4,7,'Day 7: ok: Proofread and fix errors','Check spelling, grammar, and punctuation. Ensure references and formatting match the required style.','2026-03-14','08:00:00','09:00:00',0,'2026-03-08 22:30:46');
/*!40000 ALTER TABLE `assignment_roadmap_steps` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `assignments`
--

DROP TABLE IF EXISTS `assignments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `assignments` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `title` varchar(500) NOT NULL,
  `description` text,
  `due_date` date NOT NULL,
  `priority` enum('low','medium','high') DEFAULT 'medium',
  `status` enum('pending','in_progress','completed') DEFAULT 'pending',
  `document_path` varchar(500) DEFAULT NULL,
  `document_text` text,
  `roadmap_json` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_assignments_user_due` (`user_id`,`due_date`),
  CONSTRAINT `assignments_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `assignments`
--

LOCK TABLES `assignments` WRITE;
/*!40000 ALTER TABLE `assignments` DISABLE KEYS */;
INSERT INTO `assignments` VALUES (4,1,'ok','kkk','2026-03-14','medium','pending','Assignments\\4\\OOP-ASSESSMENT-#1-2026-GROUP-A-B-C.pdf','Modern Programming Principles and Practice Semester 2 Assessment #1 Module Title: Modern Programming Principles and Practice Assessment Type: Practical Assessment – Individual collaborating Weighting: 30% Maximal Possible Mark: 100 marks Final Submission Date: AS PER MOODLE Weekly Submission Dates start: AS PER MOODLE GITHUB REPO: oop-s2-1-mvc-<student-number> e.g. (oop-s2-1-mvc-1234) Share to: JohnRowleyDorsetCollege (on github)\r\nAssignment 1: “Community Library Desk” Focus: EF Core basics + relationships, fake data, searching/filtering, Role creation UI, GitHub Actions CI. Scenario A small community library wants a simple internal system to track Books, Members, and Loans. Staff use the app to lend books to members and check what’s overdue. Required tech • ASP.NET Core MVC • EF Core + any sql variant (SQL Server, MySQL, Sqlite) • Identity already set up • xUnit tests • GitHub Actions CI workflow (build + test) Entities (exactly 3 + relationships) 1. Book o Id, Title, Author, Isbn, Category, IsAvailable 2. Member o Id, FullName, Email, Phone 3. Loan o Id, BookId, MemberId, LoanDate, DueDate, ReturnedDate (nullable) Relationships: • Book 1—* Loan • Member 1—* Loan • Loan belongs to exactly one Book and one Member Minimum features A) Admin-only Role Management page (required) Create a page /Admin/Roles that allows: • List roles • Create role (textbox + button)\r\n• Delete role Rules • Only Admin can access this page (server-side enforcement). • Seed an Admin user + Admin role so the page is accessible from day one. B) CRUD + simple workflows • Books: list + create + edit + delete • Members: list + create + edit + delete • Loans: o Create a loan (choose member + choose available book) o Mark returned (set ReturnedDate) o Prevent lending a book that is already on an active loan (ReturnedDate null) C) Fake/seed data On first run (or via migration/seed): • 20 books • 10 members • 15 loans (some returned, some active, some overdue) Use Bogus (recommended). D) Searching + filtering (mandatory, on one page) On Books index page implement: • Search by Title or Author (contains) • Filter by Category (dropdown) • Filter by Availability (All / Available / On loan) • Sorting optional (Title A–Z) This must be done with EF Core query composition (IQueryable), not in-memory filtering. E) CI workflow (mandatory) • .github/workflows/ci.yml runs restore/build/test on push + PR to main. Minimum tests (xUnit)\r\nAt least 5 tests, for example: • Cannot create a loan for a book already on an active loan • Returned loan makes book available again • Book search returns expected matches • Overdue logic: DueDate < Today and ReturnedDate is null • Role page authorization (optional if they can test policies; otherwise controller action guards) Marking • EF Core model + migrations + relationships: 30 • Fake data seeding: 16 • Books search/filter page: 20 • Loans workflow rules: 14 • Admin Role Management page: 10 • CI workflow passes on GitHub: 10\r\nMarking Rubric Community Library Desk System Total: 100 Marks Criterion 1: EF Core Model & Relationships (30 marks) Level Description Marks Correct 3-entity design (Book, Member, Loan). Proper foreign keys and navigation properties. Relationships enforced correctly. Excellent 26–30 Migrations included and database builds cleanly. Business rules (e.g., no duplicate active loan) correctly implemented. Relationships mostly correct. Minor configuration or structural Good 18–25 issues. Database builds successfully. Basic persistence works but relational design is weak or partially Satisfactory 10–17 incorrect. Poor Major relational errors, missing migrations, or broken persistence. 0–9 Criterion 2: Fake Data / Seeding (16 marks) Level Description Marks Meaningful and realistic seed data created (books, members, Excellent 14–16 loans). Includes active, returned, and overdue scenarios. Good Seed data present with reasonable variety. 10–13 Satisfactory Minimal or simplistic seed data. 5–9 Poor No meaningful seed data. 0–4 Criterion 3: Search & Filtering Implementation (20 marks)\r\nLevel Description Marks Search by title/author implemented correctly using EF query Excellent composition (IQueryable). Filtering by category and availability 17–20 works reliably. No in-memory filtering. Clean UI integration. Search and filtering mostly correct but minor inefficiencies or Good 12–16 logic gaps. Basic search OR filtering implemented but incomplete or partially Satisfactory 6–11 functional. Poor No working search/filter functionality. 0–5 Criterion 4: Loan Workflow & Business Rules (14 marks) Level Description Marks Prevents duplicate active loans. Returned books become Excellent 12–14 available again. Overdue logic works correctly and consistently. Good Most business rules work but minor logic gaps. 8–11 Satisfactory Basic loan creation works but lacks robust rule enforcement. 4–7 Poor Loan workflow unreliable or incorrect. 0–3 Criterion 5: Admin Role Management Page (10 marks) Level Description Marks Admin-only role management page implemented. Roles can be Excellent 9–10 listed and created. Server-side authorization correctly enforced. Good Role creation works but minor authorization or usability gaps. 6–8 Satisfactory Page exists but incomplete or weakly secured. 3–5 Poor No meaningful role management functionality. 0–2 Criterion 6: GitHub Repository & CI Workflow (10 marks)\r\nLevel Description Marks Clean structure (src/tests). README complete. GitHub Actions Excellent workflow builds in Release and runs tests successfully on main 9–10 branch. Good CI present but minor issues or unclear documentation. 6–8 Satisfactory Repository usable but incomplete CI or inconsistent structure. 3–5 Poor No CI workflow or failing pipeline on main. 0–2',NULL,'2026-03-08 22:10:38','2026-03-08 23:02:54'),(5,1,'yh','','2026-03-11','medium','pending','Assignments\\5\\OOP-ASSESSMENT-#2-2026-GROUP-A-B-C.pdf','Modern Programming Principles and Practice Semester 2 Assessment #2 Module Title: Modern Programming Principles and Practice Assessment Type: Practical Assessment – Individual Weighting: 30% Maximal Possible Mark: 100 marks Final Submission Date: AS PER MOODLE Weekly Submission Dates start: AS PER MOODLE Github Repo – oop-s2-2-mvc-<student-number> e.g. (oop-s2-2-mvc-1234) Share To: JohnRowleyDorsetCollege\r\nAssignment 2: “Food Safety Inspection Tracker” Focus: Serilog logging, error handling, “audit trail” thinking, and building maintainable service-based code that will scale up to your main project. Scenario A local council tracks food premises inspections. Inspectors record inspection outcomes and follow-ups. Management wants visibility into what’s happening and when things fail. Required tech • ASP.NET Core MVC • EF Core + SQLite (or SQL Server) • Identity roles • Serilog logging (mandatory) • xUnit tests (lighter than assignment 1 is fine) • GitHub Actions CI Entities Use 3 entities. 1. Premises o Id, Name, Address, Town, RiskRating (Low/Medium/High) 2. Inspection o Id, PremisesId, InspectionDate, Score (0–100), Outcome (Pass/Fail), Notes 3. FollowUp o Id, InspectionId, DueDate, Status (Open/Closed), ClosedDate (nullable) Relationships: • Premises 1—* Inspection • Inspection 1—* FollowUp Roles & access • Admin: full access\r\n• Inspector: can create inspections + follow-ups • Viewer (or Manager): read-only dashboards (no create/edit) Key features A) Serilog logging (mandatory, assessed) Set up Serilog with: • Console sink • Rolling file sink (daily) • Enriched properties: Application, Environment, and UserName (from HttpContext when available) Log at appropriate levels: • Information: create/update actions (e.g., “Inspection created”, include PremisesId, InspectionId) • Warning: validation/business rule issues (e.g., creating FollowUp with due date before inspection date) • Error: caught exceptions (include exception details) Minimum requirement: at least 8 meaningful log events across the app’s main workflows. B) Global error handling + “friendly failures” • Use a global exception handler (or middleware) so unhandled exceptions show a friendly error page. • Ensure exceptions are logged with Serilog. C) A dashboard page with aggregations Create a /Dashboard page: • Count of inspections this month • Count of failed inspections this month • Count of open follow-ups overdue (DueDate < Today AND Status Open) • Filter by Town and/or RiskRating This forces them to write grouped queries and think about the “reporting” style they’ll need in the main project. D) Fake/seed data\r\nSeed: • 12 premises across 3 towns • 25 inspections across different dates • 10 follow-ups (some overdue, some closed) E) CI workflow (repeat) Same: build + test. Minimum tests (xUnit) At least 4 tests, e.g.: • Overdue follow-ups query returns correct items • Follow-up cannot be closed without ClosedDate • Dashboard counts consistent with known seed data (or use in-memory DB) • Basic role authorization for Inspector vs Viewer (if feasible) Marking) • Serilog configured correctly + file output + enrichment: 30 • Logging coverage in workflows (not spam): 20 • Dashboard queries + filtering: 20 • EF Core model + migrations + seed data: 20 • CI passing + tests present: 10\r\nMarking Rubric Food Safety Inspection Tracker Total: 100 Marks Criterion 1: Serilog Configuration & Structured Logging (30 marks) Level Description Marks Serilog correctly configured with console + rolling file sink. Structured logging used (e.g., IDs, user context). Appropriate log Excellent 26–30 levels applied consistently. Logging integrated across main workflows. Serilog configured correctly but enrichment or structure partially Good 18–25 applied. Basic logging implemented but limited structure or inconsistent Satisfactory 10–17 log levels. Poor Logging missing, misconfigured, or not integrated meaningfully. 0–9 Criterion 2: Dashboard Queries & Filtering (20 marks) Level Description Marks Correct aggregation queries (monthly inspections, failed Excellent inspections, overdue follow-ups). Filtering by Town/RiskRating 17–20 works correctly. Efficient EF queries used. Good Dashboard mostly correct but minor logic or filtering gaps. 12–16 Satisfactory Partial aggregation implemented or limited filtering. 6–11 Poor Dashboard missing or incorrect. 0–5 Criterion 3: EF Core Model & Relationships (20 marks)\r\nLevel Description Marks Correct entity relationships (Premises → Inspection → FollowUp). Excellent Clean migrations. Seed data realistic and supports dashboard 17–20 logic. Good Mostly correct design with minor configuration issues. 12–16 Satisfactory Basic persistence but relational integrity partially weak. 6–11 Poor Broken relationships or missing migrations. 0–5 Criterion 4: Error Handling & Logging of Failures (10 marks) Level Description Marks Global exception handling implemented. Exceptions logged with Excellent 9–10 context. Friendly error page displayed. Good Exceptions logged but error handling incomplete. 6–8 Satisfactory Basic try/catch usage but inconsistent logging. 3–5 Poor Unhandled exceptions or no meaningful logging of errors. 0–2 Criterion 5: Role-Based Access Control (10 marks) Level Description Marks Clear separation between Admin, Inspector, and Viewer roles. Excellent 9–10 Server-side authorization correctly enforced. Good Roles implemented but minor access-control gaps. 6–8 Satisfactory Basic roles present but weak enforcement. 3–5 Poor No meaningful authorization rules. 0–2 Criterion 6: GitHub & CI Workflow (10 marks)\r\nLevel Description Marks CI workflow builds and tests successfully in Release Excellent 9–10 configuration. Repository cleanly structured. Good CI present but minor issues. 6–8 Satisfactory Partial CI or inconsistent repository structure. 3–5 Poor Missing or failing CI workflow. 0–2','[\n  {\n    \"step\": \"Set up project structure and repository\",\n    \"description\": \"Create the GitHub repository with the specified name and structure, ensuring proper folder organization for MVC, services, models, tests, and documentation.\",\n    \"startDate\": \"2026-03-01\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Configure Serilog logging\",\n    \"description\": \"Implement Serilog with console sink, rolling file sink, and enrichments for user context and application metadata. Ensure logs are structured and include key events.\",\n    \"startDate\": \"2026-03-02\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Develop and seed data for premises and inspections\",\n    \"description\": \"Generate realistic seed data for 12 premises across 3 towns and 25 inspections, populating the database to support dashboard aggregation and reporting.\",\n    \"startDate\": \"2026-03-03\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Implement role-based access control\",\n    \"description\": \"Define and apply Identity roles (Admin, Inspector, Viewer) to enforce access control, ensuring proper authorization checks in controllers and services.\",\n    \"startDate\": \"2026-03-12\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Build and configure EF Core migrations\",\n    \"description\": \"Create and apply migrations to set up the database schema, ensuring relationships and constraints are correctly defined for the application.\",\n    \"startDate\": \"2026-03-13\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Write unit tests for core functionality\",\n    \"description\": \"Develop at least four xUnit tests covering key features such as logging, error handling, dashboard data retrieval, and role-based access.\",\n    \"startDate\": \"2026-03-14\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Set up CI/CD workflow\",\n    \"description\": \"Configure GitHub Actions for building, testing, and deploying the application, ensuring all components pass tests before merging.\",\n    \"startDate\": \"2026-03-15\",\n    \"endDate\": \"2026-03-11\"\n  },\n  {\n    \"step\": \"Perform final review and documentation\",\n    \"description\": \"Review all components for adherence to requirements, document configurations, and prepare a summary report for the project team.\",\n    \"startDate\": \"2026-03-16\",\n    \"endDate\": \"2026-03-11\"\n  }\n]','2026-03-08 22:21:32','2026-03-08 23:30:47');
/*!40000 ALTER TABLE `assignments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `routine_slots`
--

DROP TABLE IF EXISTS `routine_slots`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `routine_slots` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `day_of_week` tinyint NOT NULL COMMENT '0=Sunday, 1=Monday, ... 6=Saturday',
  `start_time` time NOT NULL,
  `end_time` time NOT NULL,
  `title` varchar(255) NOT NULL,
  `description` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_routine_slots_user_day` (`user_id`,`day_of_week`),
  CONSTRAINT `routine_slots_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `routine_slots`
--

LOCK TABLES `routine_slots` WRITE;
/*!40000 ALTER TABLE `routine_slots` DISABLE KEYS */;
INSERT INTO `routine_slots` VALUES (1,1,1,'09:00:00','22:00:00','job','','2026-03-08 20:46:31','2026-03-08 22:17:13'),(2,1,0,'09:00:00','22:00:00','job','','2026-03-08 22:17:32','2026-03-08 22:17:32'),(3,1,2,'09:00:00','22:00:00','job','','2026-03-08 22:17:41','2026-03-08 22:17:41'),(4,1,3,'09:00:00','22:00:00','job','','2026-03-08 22:17:49','2026-03-08 22:17:49'),(5,1,4,'09:00:00','22:00:00','job','','2026-03-08 22:17:57','2026-03-08 22:17:57'),(6,1,5,'09:00:00','22:00:00','job','','2026-03-08 22:18:06','2026-03-08 22:18:06'),(7,1,6,'09:00:00','22:00:00','job','','2026-03-08 22:18:14','2026-03-08 22:18:14');
/*!40000 ALTER TABLE `routine_slots` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `id` int NOT NULL AUTO_INCREMENT,
  `email` varchar(255) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `full_name` varchar(255) NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'test@test.com','$2a$12$BTVbvn1Gf.LfuZvf/NhKPegPq/hKtemgCRIBEB7EdtmsaHAS6xDjS','tet','2026-03-08 20:38:40','2026-03-08 20:38:40');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-08 23:41:30
