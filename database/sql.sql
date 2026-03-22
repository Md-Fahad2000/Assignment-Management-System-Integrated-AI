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
) ENGINE=InnoDB AUTO_INCREMENT=37 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `ai_schedules`
--

LOCK TABLES `ai_schedules` WRITE;
/*!40000 ALTER TABLE `ai_schedules` DISABLE KEYS */;
INSERT INTO `ai_schedules` VALUES (21,1,NULL,'2026-03-08','08:00:00','09:00:00','yh','2026-03-09 00:15:13'),(22,1,NULL,'2026-03-09','08:00:00','09:00:00','yh','2026-03-09 00:15:13'),(23,1,NULL,'2026-03-08','08:00:00','09:00:00','ok','2026-03-09 00:15:13'),(24,1,NULL,'2026-03-09','08:00:00','09:00:00','ok','2026-03-09 00:15:13'),(25,1,NULL,'2026-03-08','08:00:00','09:00:00','test','2026-03-09 00:15:13'),(26,1,NULL,'2026-03-09','08:00:00','09:00:00','test','2026-03-09 00:15:13'),(33,1,10,'2026-03-14','08:00:00','09:00:00','test','2026-03-15 04:11:03'),(35,1,10,'2026-03-15','08:00:00','09:00:00','Implementing Community Library Desk System Features','2026-03-15 05:21:09'),(36,1,10,'2026-03-16','08:00:00','09:00:00','Implementing Community Library Desk System Features','2026-03-15 05:21:09');
/*!40000 ALTER TABLE `ai_schedules` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `assignment_ai_responses`
--

DROP TABLE IF EXISTS `assignment_ai_responses`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `assignment_ai_responses` (
  `id` int NOT NULL AUTO_INCREMENT,
  `assignment_id` int NOT NULL,
  `user_id` int NOT NULL,
  `response_type` varchar(50) NOT NULL,
  `response_value` longtext NOT NULL,
  `updated_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_assignment_user_type` (`assignment_id`,`user_id`,`response_type`),
  KEY `idx_assignment_user` (`assignment_id`,`user_id`),
  KEY `fk_ai_resp_user` (`user_id`),
  CONSTRAINT `fk_ai_resp_assignment` FOREIGN KEY (`assignment_id`) REFERENCES `assignments` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_ai_resp_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `assignment_ai_responses`
--

LOCK TABLES `assignment_ai_responses` WRITE;
/*!40000 ALTER TABLE `assignment_ai_responses` DISABLE KEYS */;
INSERT INTO `assignment_ai_responses` VALUES (1,10,1,'quiz','Q1: What is the primary purpose of the Community Library Desk System?\nA1: The primary purpose of the Community Library Desk System is to track Books, Members, and Loans, allowing staff to lend books to members and check what\'s overdue.\n\nQ2: Which technologies are required for the implementation of the Community Library Desk System?\nA2: The required technologies include ASP.NET Core MVC, EF Core + any SQL variant (SQL Server, MySQL, Sqlite), Identity already set up, xUnit tests, and GitHub Actions CI workflow.\n\nQ3: What are the three entities in the Community Library Desk System?\nA3: The three entities in the Community Library Desk System are Book, Member, and Loan.\n\nQ4: What is the relationship between the Book and Loan entities?\nA4: The Book entity has a one-to-many relationship with the Loan entity, meaning one book can have multiple loans.\n\nQ5: What is the relationship between the Member and Loan entities?\nA5: The Member entity has a one-to-many relationship with the Loan entity, meaning one member can have multiple loans.\n\nQ6: What is the minimum feature required for the Admin Role Management page?\nA6: The minimum feature required for the Admin Role Management page is to list roles, create a role (textbox + button), and delete a role.\n\nQ7: What is the purpose of the CI workflow in the Community Library Desk System?\nA7: The purpose of the CI workflow is to run restore, build, and test on push + PR to main, ensuring that the system is properly tested and validated before deployment.','2026-03-15 05:18:37'),(2,10,1,'key_dates','[\"No dates found.\"]','2026-03-15 06:58:09'),(4,10,1,'key_points','[\"Here are the key points from the assignment:\",\"EF Core basics \\u002B relationships, fake data, searching/filtering, Role creation UI, GitHub Actions CI\",\"Implement a Community Library Desk system to track Books, Members, and Loans\",\"Required tech: ASP.NET Core MVC, EF Core \\u002B any sql variant, Identity, xUnit tests, GitHub Actions CI workflow\",\"Entities: Book, Member, Loan with relationships: Book 1\\u2014* Loan, Member 1\\u2014* Loan, Loan belongs to exactly one Book and one Member\",\"Minimum features: Admin-only Role Management page, CRUD \\u002B simple workflows for Books, Members, and Loans, Fake/seed data, Searching \\u002B filtering on Books index page\",\"CI workflow: .github/workflows/ci.yml runs restore/build/test on push \\u002B PR to main\",\"Marking criteria: EF Core model \\u002B migrations \\u002B relationships, Fake data seeding, Books search/filter page, Loans workflow rules, Admin Role Management page, CI workflow passes on GitHub\",\"Entities must have proper foreign keys and navigation properties, relationships must be enforced correctly\",\"Database builds cleanly, business rules must be correctly implemented\",\"Seed data must be meaningful and realistic, including active, returned, and overdue scenarios\",\"Search by title/author must be implemented correctly using EF query composition, filtering by category and availability must work reliably\",\"Prevent duplicate active loans, returned books must become available again, overdue logic must work correctly and consistently\",\"Admin-only role management page must be implemented, roles can be listed and created, server-side authorization must be correctly enforced\",\"GitHub repository must have a clean structure, README must be complete, GitHub Actions workflow must build in Release and run tests successfully on main branch\"]','2026-03-15 05:19:19'),(5,10,1,'suggested_title','Community Library Desk System Implementation','2026-03-15 05:19:42'),(7,11,2,'key_dates','[]','2026-03-22 15:07:55');
/*!40000 ALTER TABLE `assignment_ai_responses` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `assignment_chat_messages`
--

DROP TABLE IF EXISTS `assignment_chat_messages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `assignment_chat_messages` (
  `id` int NOT NULL AUTO_INCREMENT,
  `assignment_id` int NOT NULL,
  `user_id` int NOT NULL,
  `role` varchar(20) NOT NULL,
  `content` text NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  `rating` tinyint DEFAULT NULL COMMENT '1 = thumbs up, -1 = thumbs down',
  PRIMARY KEY (`id`),
  KEY `idx_assignment_user` (`assignment_id`,`user_id`,`created_at`),
  KEY `fk_chat_user` (`user_id`),
  CONSTRAINT `fk_chat_assignment` FOREIGN KEY (`assignment_id`) REFERENCES `assignments` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_chat_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `assignment_chat_messages`
--

LOCK TABLES `assignment_chat_messages` WRITE;
/*!40000 ALTER TABLE `assignment_chat_messages` DISABLE KEYS */;
INSERT INTO `assignment_chat_messages` VALUES (5,10,1,'user','How can I finish before the deadline?','2026-03-15 04:07:16',NULL),(6,10,1,'assistant','**How to finish before the 2026‑03‑28 deadline while respecting your job schedule (09:00‑22:00 every day)**  \n\nYour only available work windows are:  \n\n* **Early‑morning:** 05:00 – 09:00 (4 h)  \n* **Late‑night:** 22:00 – 02:00 (4 h)  \n\nYou can split each day’s work into two 4‑hour blocks (or use just one block if you prefer). Below is a concrete, day‑by‑day plan that follows the AI‑generated roadmap and stays completely outside the 09:00‑22:00 job period.\n\n| Date (2026) | Roadmap step | What to do (in the available windows) | Suggested time block |\n|-------------|--------------|----------------------------------------|----------------------|\n| **Mon 15 Mar** | Step 1 – Project setup | • Create ASP.NET Core MVC project with Identity.<br>• Add EF Core (SQLite).<br>• Init GitHub repo, push initial commit. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Tue 16 Mar** | Step 2 – Entities & migrations | • Define Book, Member, Loan classes with properties & navigation.<br>• Configure relationships in DbContext.<br>• Add‑migration & update database. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Wed 17 Mar** | Step 3 – Fake data seeding | • Install Bogus.<br>• Write seed generator for 20 books, 10 members, 15 loans (mix of returned/active/overdue).<br>• Hook seed into DbContext.OnModelCreating or a hosted service. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Thu 18 Mar** | Step 4 – Books CRUD | • Scaffold (or manually create) BooksController with Index, Create, Edit, Delete.<br>• Add corresponding Razor views.<br>• Basic validation. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Fri 19 Mar** | Step 5 – Members CRUD | • Replicate the Books CRUD pattern for Members.<br>• Verify navigation to/from Loans works. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Sat 20 Mar** | Step 6 – Loans workflow (part 1) | • Build LoansController Create action: dropdowns for Member & available books (IsAvailable = true).<br>• Implement logic to block creation if the selected book already has an active loan (ReturnedDate null). | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Sun 21 Mar** | Step 6 – Loans workflow (part 2) | • Add “Mark returned” action that sets ReturnedDate and updates Book.IsAvailable.<br>• Test both paths. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Mon 22 Mar** | Step 7 – Books search & filter | • On Books Index add search box (Title/Author contains).<br>• Add Category dropdown (distinct values from DB).<br>• Add Availability dropdown (All/Available/On loan).<br>• Compose query with IQueryable based on selected filters; no .ToList() before filtering.<br>• Optional Title A‑Z sort. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Tue 23 Mar** | Step 7 – Refine search/filter | • Ensure dropdowns repopulate after post‑back.<br>• Verify EF query is sent to SQL (check with logging).<br>• Polish UI (Bootstrap). | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Wed 24 Mar** | Step 8 – Admin role management | • Create AdminController with Roles action (GET list, POST create, POST delete).<br>• Secure with `[Authorize(Roles = \"Admin\")]`.<br>• Seed an Admin user + Admin role in DbContext.OnModelCreating (or migration). | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Thu 25 Mar** | Step 8 – Test role page | • Log in as the seeded Admin, verify you can list/create/delete roles.<br>• Log in as a non‑Admin, confirm 403/redirect. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Fri 26 Mar** | Step 9 – xUnit tests (set 1) | • Write test 1: Attempt to create a loan for a book with an active loan → expect failure.<br>• Write test 2: Return a loan → verify Book.IsAvailable becomes true.<br>• Configure test project to use an in‑memory SQLite database. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Sat 27 Mar** | Step 9 – xUnit tests (set 2) & CI workflow | • Write test 3: Search by title/author returns expected matches.<br>• Write test 4: Overdue logic (DueDate < Today && ReturnedDate == null).<br>• (Optional) test 5: Authorize attribute on Admin/Roles.<br>• Add `.github/workflows/ci.yml`: `dotnet restore`, `dotnet build --configuration Release`, `dotnet test`.<br>• Push to GitHub; verify the workflow runs on push/PR. | 05:00‑09:00 **or** 22:00‑02:00 |\n| **Sun 28 Mar** | Step 10 – Final integration & submission | • Run the full application locally: create loans, search/filter, return books, role management.<br>• Fix any bugs discovered.<br>• Ensure GitHub Actions shows a green check on the main branch.<br>• Prepare final ZIP or repo link and submit via Moodle before the deadline (end of day). | 05:00‑09:00 **or** 22:00‑02:00 |\n\n### Tips to stay productive within the limited windows\n1. **Batch similar tasks** – e.g., do all controller scaffolding in one morning block, then all view work the next night.\n2. **Use timers** – work in 45‑minute focused bursts with a 15‑minute break to avoid fatigue.\n3. **Commit frequently** – after each logical change (e.g., after finishing the Book CRUD) push to GitHub; this gives you a safety net and keeps the CI workflow happy.\n4. **Leverage logging** – while working late‑night, enable EF Core logging to see the generated SQL; this helps confirm that search/filter stays in the database layer.\n5. **Check the CI early** – as soon as you push the workflow file (Step 9), watch the Actions tab; if it fails, fix it in the next available block rather than letting errors accumulate.\n\nBy following the schedule above, each roadmap step gets a dedicated 4‑hour slot, well outside your 09:00‑22:00 job hours, and you finish all work on **2026‑03‑28**, meeting the deadline. Good luck!','2026-03-15 04:07:16',NULL),(7,10,1,'user','Explain step 8 (\"Step 8\") in simple terms.','2026-03-15 04:28:35',NULL),(8,10,1,'assistant','**Step 8: Admin Role Management**\n\n**Goal:** Create a secure page for managing roles (like \"Admin\") in your Community Library Desk application.\n\n**What to do:**\n\n1. **Create a new controller**: Make a new class called `AdminController` that will handle requests related to roles.\n2. **Add a \"Roles\" action**: In this controller, create a method called `Roles` that will display a page showing all roles.\n3. **Add a \"Create role\" feature**: On this page, add a form that allows users to create new roles. This form should include a text box for the role name and a button to submit the new role.\n4. **Add a \"Delete role\" feature**: Also on this page, add a button or link to delete existing roles.\n5. **Secure the page**: Use an attribute called `[Authorize(Roles = \"Admin\")]` to ensure that only users with the \"Admin\" role can access this page.\n6. **Seed an Admin user and role**: In your database, create an initial Admin user and role so that the page is accessible from the start.\n\n**Why:** This step is important because it allows administrators to manage roles in the system, which is crucial for controlling access and permissions. By securing the page with the `[Authorize]` attribute, you ensure that only authorized users can access this sensitive functionality.','2026-03-15 04:28:35',NULL),(9,10,1,'user','What should I focus on right now?','2026-03-15 05:22:02',NULL),(10,10,1,'assistant','Right now, you should focus on:  \n\n### **Step 2: Define Book, Member, Loan entities and create the first database migration**  \n*(Due: 2026-03-16 — this is tomorrow)*  \n\n**Why now?**  \nThis is the **foundation** of your whole project. If the entities and relationships are wrong, everything else (CRUD, loans, search, seeding, tests) will break. Fixing this later will waste hours.  \n\n---\n\n### ✅ **What to do tonight (22:00–02:00) or tomorrow morning (05:00–09:00):**  \n\n#### **1. Create the 3 classes exactly as required:**  \nIn your `Models/` folder, make these 3 classes with **exactly** these properties:  \n\n**Book.cs**  \n```csharp\npublic class Book\n{\n    public int Id { get; set; }\n    public string Title { get; set; } = string.Empty;\n    public string Author { get; set; } = string.Empty;\n    public string Isbn { get; set; } = string.Empty;\n    public string Category { get; set; } = string.Empty;\n    public bool IsAvailable { get; set; } = true; // Default: true when added\n\n    // Navigation property (1 Book → many Loans)\n    public List<Loan> Loans { get; set; } = new();\n}\n```\n\n**Member.cs**  \n```csharp\npublic class Member\n{\n    public int Id { get; set; }\n    public string FullName { get; set; } = string.Empty;\n    public string Email { get; set; } = string.Empty;\n    public string Phone { get; set; } = string.Empty;\n\n    // Navigation property (1 Member → many Loans)\n    public List<Loan> Loans { get; set; } = new();\n}\n```\n\n**Loan.cs**  \n```csharp\npublic class Loan\n{\n    public int Id { get; set; }\n    public int BookId { get; set; }\n    public int MemberId { get; set; }\n    public DateTime LoanDate { get; set; }\n    public DateTime DueDate { get; set; }\n    public DateTime? ReturnedDate { get; set; } // Nullable — key for \"active\" loans\n\n    // Navigation properties (Loan belongs to exactly 1 Book and 1 Member)\n    public Book Book { get; set; } = null!;\n    public Member Member { get; set; } = null!;\n}\n```\n\n> ? **Important**:  \n> - Use `DateTime?` for `ReturnedDate` — this is how you know if a loan is still active (null = not returned).  \n> - `BookId` and `MemberId` are **foreign keys** — EF Core will auto-link them to the navigation properties (`Book` and `Member`).  \n\n---\n\n#### **2. Configure relationships in `DbContext`**  \nIn `Data/ApplicationDbContext.cs`, inside `OnModelCreating()`, add:  \n\n```csharp\nprotected override void OnModelCreating(ModelBuilder modelBuilder)\n{\n    // Loan is the \"junction\" table — configure relationships\n    modelBuilder.Entity<Loan>()\n        .HasOne(l => l.Book)\n        .WithMany(b => b.Loans)\n        .HasForeignKey(l => l.BookId)\n        .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a book if it has active loans\n\n    modelBuilder.Entity<Loan>()\n        .HasOne(l => l.Member)\n        .WithMany(m => m.Loans)\n        .HasForeignKey(l => l.MemberId)\n        .OnDelete(DeleteBehavior.Restrict);\n}\n```\n\n> ? This tells EF Core:  \n> - A Loan must have one Book and one Member.  \n> - You can’t accidentally delete a Book if it’s on loan.  \n\n---\n\n#### **3. Create and apply the migration**  \nOpen **Terminal** in your project folder and run:  \n\n```bash\ndotnet ef migrations add InitialCreate\ndotnet ef database update\n```\n\n✅ **Check**:  \n- A new folder `Migrations/` should appear with a `.cs` file.  \n- Your database (SQLite file) should be created in `bin/Debug/net8.0/` (or similar).  \n\n> ✅ **Done?** Then you’ve completed Step 2.  \n> **Next**: Step 3 (Fake data with Bogus) — start that after this.  \n\n---\n\n### ? Don’t skip this!  \nIf you get this wrong:  \n- Your loans won’t link to books/members.  \n- Your search/filter won’t work.  \n- Your tests will fail.  \n- You’ll lose 30/100 marks.  \n\n**Focus on this now — nothing else matters until Step 2 is 100% correct.**  \nYou’ve got this — just 2–3 focused hours tonight or tomorrow morning.','2026-03-15 05:22:02',NULL);
/*!40000 ALTER TABLE `assignment_chat_messages` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `assignments`
--

LOCK TABLES `assignments` WRITE;
/*!40000 ALTER TABLE `assignments` DISABLE KEYS */;
INSERT INTO `assignments` VALUES (10,1,'Implementing Community Library Desk System Features','','2026-03-28','high','pending','Assignments\\10\\OOP-ASSESSMENT-#1-2026-GROUP-A-B-C.pdf','Modern Programming Principles and Practice Semester 2 Assessment #1 Module Title: Modern Programming Principles and Practice Assessment Type: Practical Assessment – Individual collaborating Weighting: 30% Maximal Possible Mark: 100 marks Final Submission Date: AS PER MOODLE Weekly Submission Dates start: AS PER MOODLE GITHUB REPO: oop-s2-1-mvc-<student-number> e.g. (oop-s2-1-mvc-1234) Share to: JohnRowleyDorsetCollege (on github)\r\nAssignment 1: “Community Library Desk” Focus: EF Core basics + relationships, fake data, searching/filtering, Role creation UI, GitHub Actions CI. Scenario A small community library wants a simple internal system to track Books, Members, and Loans. Staff use the app to lend books to members and check what’s overdue. Required tech • ASP.NET Core MVC • EF Core + any sql variant (SQL Server, MySQL, Sqlite) • Identity already set up • xUnit tests • GitHub Actions CI workflow (build + test) Entities (exactly 3 + relationships) 1. Book o Id, Title, Author, Isbn, Category, IsAvailable 2. Member o Id, FullName, Email, Phone 3. Loan o Id, BookId, MemberId, LoanDate, DueDate, ReturnedDate (nullable) Relationships: • Book 1—* Loan • Member 1—* Loan • Loan belongs to exactly one Book and one Member Minimum features A) Admin-only Role Management page (required) Create a page /Admin/Roles that allows: • List roles • Create role (textbox + button)\r\n• Delete role Rules • Only Admin can access this page (server-side enforcement). • Seed an Admin user + Admin role so the page is accessible from day one. B) CRUD + simple workflows • Books: list + create + edit + delete • Members: list + create + edit + delete • Loans: o Create a loan (choose member + choose available book) o Mark returned (set ReturnedDate) o Prevent lending a book that is already on an active loan (ReturnedDate null) C) Fake/seed data On first run (or via migration/seed): • 20 books • 10 members • 15 loans (some returned, some active, some overdue) Use Bogus (recommended). D) Searching + filtering (mandatory, on one page) On Books index page implement: • Search by Title or Author (contains) • Filter by Category (dropdown) • Filter by Availability (All / Available / On loan) • Sorting optional (Title A–Z) This must be done with EF Core query composition (IQueryable), not in-memory filtering. E) CI workflow (mandatory) • .github/workflows/ci.yml runs restore/build/test on push + PR to main. Minimum tests (xUnit)\r\nAt least 5 tests, for example: • Cannot create a loan for a book already on an active loan • Returned loan makes book available again • Book search returns expected matches • Overdue logic: DueDate < Today and ReturnedDate is null • Role page authorization (optional if they can test policies; otherwise controller action guards) Marking • EF Core model + migrations + relationships: 30 • Fake data seeding: 16 • Books search/filter page: 20 • Loans workflow rules: 14 • Admin Role Management page: 10 • CI workflow passes on GitHub: 10\r\nMarking Rubric Community Library Desk System Total: 100 Marks Criterion 1: EF Core Model & Relationships (30 marks) Level Description Marks Correct 3-entity design (Book, Member, Loan). Proper foreign keys and navigation properties. Relationships enforced correctly. Excellent 26–30 Migrations included and database builds cleanly. Business rules (e.g., no duplicate active loan) correctly implemented. Relationships mostly correct. Minor configuration or structural Good 18–25 issues. Database builds successfully. Basic persistence works but relational design is weak or partially Satisfactory 10–17 incorrect. Poor Major relational errors, missing migrations, or broken persistence. 0–9 Criterion 2: Fake Data / Seeding (16 marks) Level Description Marks Meaningful and realistic seed data created (books, members, Excellent 14–16 loans). Includes active, returned, and overdue scenarios. Good Seed data present with reasonable variety. 10–13 Satisfactory Minimal or simplistic seed data. 5–9 Poor No meaningful seed data. 0–4 Criterion 3: Search & Filtering Implementation (20 marks)\r\nLevel Description Marks Search by title/author implemented correctly using EF query Excellent composition (IQueryable). Filtering by category and availability 17–20 works reliably. No in-memory filtering. Clean UI integration. Search and filtering mostly correct but minor inefficiencies or Good 12–16 logic gaps. Basic search OR filtering implemented but incomplete or partially Satisfactory 6–11 functional. Poor No working search/filter functionality. 0–5 Criterion 4: Loan Workflow & Business Rules (14 marks) Level Description Marks Prevents duplicate active loans. Returned books become Excellent 12–14 available again. Overdue logic works correctly and consistently. Good Most business rules work but minor logic gaps. 8–11 Satisfactory Basic loan creation works but lacks robust rule enforcement. 4–7 Poor Loan workflow unreliable or incorrect. 0–3 Criterion 5: Admin Role Management Page (10 marks) Level Description Marks Admin-only role management page implemented. Roles can be Excellent 9–10 listed and created. Server-side authorization correctly enforced. Good Role creation works but minor authorization or usability gaps. 6–8 Satisfactory Page exists but incomplete or weakly secured. 3–5 Poor No meaningful role management functionality. 0–2 Criterion 6: GitHub Repository & CI Workflow (10 marks)\r\nLevel Description Marks Clean structure (src/tests). README complete. GitHub Actions Excellent workflow builds in Release and runs tests successfully on main 9–10 branch. Good CI present but minor issues or unclear documentation. 6–8 Satisfactory Repository usable but incomplete CI or inconsistent structure. 3–5 Poor No CI workflow or failing pipeline on main. 0–2','[\n  {\n    \"step\": \"Step 2\",\n    \"description\": \"Define Book, Member, Loan entity classes with required properties and navigation properties. Configure relationships in DbContext. Create and apply initial migration to create database.\",\n    \"startDate\": \"2026-03-16\",\n    \"endDate\": \"2026-03-16\",\n    \"difficulty\": \"medium\",\n    \"estimatedMinutes\": 180\n  },\n  {\n    \"step\": \"Step 3\",\n    \"description\": \"Implement data seeding using Bogus library. Generate 20 books, 10 members, and 15 loans with a mix of returned, active, and overdue statuses. Ensure seed runs on database initialization.\",\n    \"startDate\": \"2026-03-17\",\n    \"endDate\": \"2026-03-17\",\n    \"difficulty\": \"medium\",\n    \"estimatedMinutes\": 150\n  },\n  {\n    \"step\": \"Step 4\",\n    \"description\": \"Implement CRUD operations for Books: Index page with list, Create, Edit, Delete actions and corresponding views. Ensure validation and error handling.\",\n    \"startDate\": \"2026-03-18\",\n    \"endDate\": \"2026-03-18\",\n    \"difficulty\": \"medium\",\n    \"estimatedMinutes\": 180\n  },\n  {\n    \"step\": \"Step 5\",\n    \"description\": \"Implement CRUD operations for Members similar to Books.\",\n    \"startDate\": \"2026-03-19\",\n    \"endDate\": \"2026-03-19\",\n    \"difficulty\": \"medium\",\n    \"estimatedMinutes\": 150\n  },\n  {\n    \"step\": \"Step 6\",\n    \"description\": \"Implement Loans functionality: Create loan action with selection of member and available book (only books with IsAvailable true). Mark loan as returned by setting ReturnedDate. Add business logic to prevent creating a loan for a book that already has an active loan (ReturnedDate null). Update book availability accordingly.\",\n    \"startDate\": \"2026-03-20\",\n    \"endDate\": \"2026-03-21\",\n    \"difficulty\": \"hard\",\n    \"estimatedMinutes\": 240\n  },\n  {\n    \"step\": \"Step 7\",\n    \"description\": \"Enhance Books Index page: Add a search box for Title or Author (contains). Add dropdown for Category filter (populate from distinct categories). Add dropdown for Availability filter (All, Available, On loan). Implement using IQueryable to compose query based on filters. Optional sorting by Title.\",\n    \"startDate\": \"2026-03-22\",\n    \"endDate\": \"2026-03-23\",\n    \"difficulty\": \"medium\",\n    \"estimatedMinutes\": 180\n  },\n  {\n    \"step\": \"Step 8\",\n    \"description\": \"Create Admin controller and Roles view at /Admin/Roles. Implement listing of roles, creating new role (textbox + button), and deleting role. Secure the page with an Admin role requirement using the Authorize attribute. Seed an initial Admin user and role in the database on startup.\",\n    \"startDate\": \"2026-03-24\",\n    \"endDate\": \"2026-03-25\",\n    \"difficulty\": \"hard\",\n    \"estimatedMinutes\": 240\n  },\n  {\n    \"step\": \"Step 9\",\n    \"description\": \"Write xUnit tests covering: 1) Cannot create loan for book already on active loan; 2) Returned loan makes book available; 3) Book search returns expected matches; 4) Overdue logic (DueDate < Today and ReturnedDate null); 5) Role page authorization (optional). Set up GitHub Actions CI workflow in .github/workflows/ci.yml to restore, build, and run tests on push and PR to main branch.\",\n    \"startDate\": \"2026-03-26\",\n    \"endDate\": \"2026-03-27\",\n    \"difficulty\": \"hard\",\n    \"estimatedMinutes\": 240\n  },\n  {\n    \"step\": \"Step 10\",\n    \"description\": \"Perform final integration testing across all features. Fix any bugs or issues. Ensure CI passes. Prepare final submission and submit via Moodle before deadline.\",\n    \"startDate\": \"2026-03-28\",\n    \"endDate\": \"2026-03-28\",\n    \"difficulty\": \"easy\",\n    \"estimatedMinutes\": 120\n  }\n]','2026-03-15 03:57:30','2026-03-15 05:21:29'),(11,2,'test','','2026-03-31','medium','in_progress','Assignments\\11\\OOP-ASSESSMENT-#1-2026-GROUP-A-B-C.pdf','Modern Programming Principles and Practice Semester 2 Assessment #1 Module Title: Modern Programming Principles and Practice Assessment Type: Practical Assessment – Individual collaborating Weighting: 30% Maximal Possible Mark: 100 marks Final Submission Date: AS PER MOODLE Weekly Submission Dates start: AS PER MOODLE GITHUB REPO: oop-s2-1-mvc-<student-number> e.g. (oop-s2-1-mvc-1234) Share to: JohnRowleyDorsetCollege (on github)\r\nAssignment 1: “Community Library Desk” Focus: EF Core basics + relationships, fake data, searching/filtering, Role creation UI, GitHub Actions CI. Scenario A small community library wants a simple internal system to track Books, Members, and Loans. Staff use the app to lend books to members and check what’s overdue. Required tech • ASP.NET Core MVC • EF Core + any sql variant (SQL Server, MySQL, Sqlite) • Identity already set up • xUnit tests • GitHub Actions CI workflow (build + test) Entities (exactly 3 + relationships) 1. Book o Id, Title, Author, Isbn, Category, IsAvailable 2. Member o Id, FullName, Email, Phone 3. Loan o Id, BookId, MemberId, LoanDate, DueDate, ReturnedDate (nullable) Relationships: • Book 1—* Loan • Member 1—* Loan • Loan belongs to exactly one Book and one Member Minimum features A) Admin-only Role Management page (required) Create a page /Admin/Roles that allows: • List roles • Create role (textbox + button)\r\n• Delete role Rules • Only Admin can access this page (server-side enforcement). • Seed an Admin user + Admin role so the page is accessible from day one. B) CRUD + simple workflows • Books: list + create + edit + delete • Members: list + create + edit + delete • Loans: o Create a loan (choose member + choose available book) o Mark returned (set ReturnedDate) o Prevent lending a book that is already on an active loan (ReturnedDate null) C) Fake/seed data On first run (or via migration/seed): • 20 books • 10 members • 15 loans (some returned, some active, some overdue) Use Bogus (recommended). D) Searching + filtering (mandatory, on one page) On Books index page implement: • Search by Title or Author (contains) • Filter by Category (dropdown) • Filter by Availability (All / Available / On loan) • Sorting optional (Title A–Z) This must be done with EF Core query composition (IQueryable), not in-memory filtering. E) CI workflow (mandatory) • .github/workflows/ci.yml runs restore/build/test on push + PR to main. Minimum tests (xUnit)\r\nAt least 5 tests, for example: • Cannot create a loan for a book already on an active loan • Returned loan makes book available again • Book search returns expected matches • Overdue logic: DueDate < Today and ReturnedDate is null • Role page authorization (optional if they can test policies; otherwise controller action guards) Marking • EF Core model + migrations + relationships: 30 • Fake data seeding: 16 • Books search/filter page: 20 • Loans workflow rules: 14 • Admin Role Management page: 10 • CI workflow passes on GitHub: 10\r\nMarking Rubric Community Library Desk System Total: 100 Marks Criterion 1: EF Core Model & Relationships (30 marks) Level Description Marks Correct 3-entity design (Book, Member, Loan). Proper foreign keys and navigation properties. Relationships enforced correctly. Excellent 26–30 Migrations included and database builds cleanly. Business rules (e.g., no duplicate active loan) correctly implemented. Relationships mostly correct. Minor configuration or structural Good 18–25 issues. Database builds successfully. Basic persistence works but relational design is weak or partially Satisfactory 10–17 incorrect. Poor Major relational errors, missing migrations, or broken persistence. 0–9 Criterion 2: Fake Data / Seeding (16 marks) Level Description Marks Meaningful and realistic seed data created (books, members, Excellent 14–16 loans). Includes active, returned, and overdue scenarios. Good Seed data present with reasonable variety. 10–13 Satisfactory Minimal or simplistic seed data. 5–9 Poor No meaningful seed data. 0–4 Criterion 3: Search & Filtering Implementation (20 marks)\r\nLevel Description Marks Search by title/author implemented correctly using EF query Excellent composition (IQueryable). Filtering by category and availability 17–20 works reliably. No in-memory filtering. Clean UI integration. Search and filtering mostly correct but minor inefficiencies or Good 12–16 logic gaps. Basic search OR filtering implemented but incomplete or partially Satisfactory 6–11 functional. Poor No working search/filter functionality. 0–5 Criterion 4: Loan Workflow & Business Rules (14 marks) Level Description Marks Prevents duplicate active loans. Returned books become Excellent 12–14 available again. Overdue logic works correctly and consistently. Good Most business rules work but minor logic gaps. 8–11 Satisfactory Basic loan creation works but lacks robust rule enforcement. 4–7 Poor Loan workflow unreliable or incorrect. 0–3 Criterion 5: Admin Role Management Page (10 marks) Level Description Marks Admin-only role management page implemented. Roles can be Excellent 9–10 listed and created. Server-side authorization correctly enforced. Good Role creation works but minor authorization or usability gaps. 6–8 Satisfactory Page exists but incomplete or weakly secured. 3–5 Poor No meaningful role management functionality. 0–2 Criterion 6: GitHub Repository & CI Workflow (10 marks)\r\nLevel Description Marks Clean structure (src/tests). README complete. GitHub Actions Excellent workflow builds in Release and runs tests successfully on main 9–10 branch. Good CI present but minor issues or unclear documentation. 6–8 Satisfactory Repository usable but incomplete CI or inconsistent structure. 3–5 Poor No CI workflow or failing pipeline on main. 0–2',NULL,'2026-03-22 15:07:34','2026-03-22 15:44:59'),(12,2,'ydhhh','hj','2026-03-31','medium','in_progress','Assignments\\12\\OOP-ASSESSMENT-#2-2026-GROUP-A-B-C.pdf','Modern Programming Principles and Practice Semester 2 Assessment #2 Module Title: Modern Programming Principles and Practice Assessment Type: Practical Assessment – Individual Weighting: 30% Maximal Possible Mark: 100 marks Final Submission Date: AS PER MOODLE Weekly Submission Dates start: AS PER MOODLE Github Repo – oop-s2-2-mvc-<student-number> e.g. (oop-s2-2-mvc-1234) Share To: JohnRowleyDorsetCollege\r\nAssignment 2: “Food Safety Inspection Tracker” Focus: Serilog logging, error handling, “audit trail” thinking, and building maintainable service-based code that will scale up to your main project. Scenario A local council tracks food premises inspections. Inspectors record inspection outcomes and follow-ups. Management wants visibility into what’s happening and when things fail. Required tech • ASP.NET Core MVC • EF Core + SQLite (or SQL Server) • Identity roles • Serilog logging (mandatory) • xUnit tests (lighter than assignment 1 is fine) • GitHub Actions CI Entities Use 3 entities. 1. Premises o Id, Name, Address, Town, RiskRating (Low/Medium/High) 2. Inspection o Id, PremisesId, InspectionDate, Score (0–100), Outcome (Pass/Fail), Notes 3. FollowUp o Id, InspectionId, DueDate, Status (Open/Closed), ClosedDate (nullable) Relationships: • Premises 1—* Inspection • Inspection 1—* FollowUp Roles & access • Admin: full access\r\n• Inspector: can create inspections + follow-ups • Viewer (or Manager): read-only dashboards (no create/edit) Key features A) Serilog logging (mandatory, assessed) Set up Serilog with: • Console sink • Rolling file sink (daily) • Enriched properties: Application, Environment, and UserName (from HttpContext when available) Log at appropriate levels: • Information: create/update actions (e.g., “Inspection created”, include PremisesId, InspectionId) • Warning: validation/business rule issues (e.g., creating FollowUp with due date before inspection date) • Error: caught exceptions (include exception details) Minimum requirement: at least 8 meaningful log events across the app’s main workflows. B) Global error handling + “friendly failures” • Use a global exception handler (or middleware) so unhandled exceptions show a friendly error page. • Ensure exceptions are logged with Serilog. C) A dashboard page with aggregations Create a /Dashboard page: • Count of inspections this month • Count of failed inspections this month • Count of open follow-ups overdue (DueDate < Today AND Status Open) • Filter by Town and/or RiskRating This forces them to write grouped queries and think about the “reporting” style they’ll need in the main project. D) Fake/seed data\r\nSeed: • 12 premises across 3 towns • 25 inspections across different dates • 10 follow-ups (some overdue, some closed) E) CI workflow (repeat) Same: build + test. Minimum tests (xUnit) At least 4 tests, e.g.: • Overdue follow-ups query returns correct items • Follow-up cannot be closed without ClosedDate • Dashboard counts consistent with known seed data (or use in-memory DB) • Basic role authorization for Inspector vs Viewer (if feasible) Marking) • Serilog configured correctly + file output + enrichment: 30 • Logging coverage in workflows (not spam): 20 • Dashboard queries + filtering: 20 • EF Core model + migrations + seed data: 20 • CI passing + tests present: 10\r\nMarking Rubric Food Safety Inspection Tracker Total: 100 Marks Criterion 1: Serilog Configuration & Structured Logging (30 marks) Level Description Marks Serilog correctly configured with console + rolling file sink. Structured logging used (e.g., IDs, user context). Appropriate log Excellent 26–30 levels applied consistently. Logging integrated across main workflows. Serilog configured correctly but enrichment or structure partially Good 18–25 applied. Basic logging implemented but limited structure or inconsistent Satisfactory 10–17 log levels. Poor Logging missing, misconfigured, or not integrated meaningfully. 0–9 Criterion 2: Dashboard Queries & Filtering (20 marks) Level Description Marks Correct aggregation queries (monthly inspections, failed Excellent inspections, overdue follow-ups). Filtering by Town/RiskRating 17–20 works correctly. Efficient EF queries used. Good Dashboard mostly correct but minor logic or filtering gaps. 12–16 Satisfactory Partial aggregation implemented or limited filtering. 6–11 Poor Dashboard missing or incorrect. 0–5 Criterion 3: EF Core Model & Relationships (20 marks)\r\nLevel Description Marks Correct entity relationships (Premises → Inspection → FollowUp). Excellent Clean migrations. Seed data realistic and supports dashboard 17–20 logic. Good Mostly correct design with minor configuration issues. 12–16 Satisfactory Basic persistence but relational integrity partially weak. 6–11 Poor Broken relationships or missing migrations. 0–5 Criterion 4: Error Handling & Logging of Failures (10 marks) Level Description Marks Global exception handling implemented. Exceptions logged with Excellent 9–10 context. Friendly error page displayed. Good Exceptions logged but error handling incomplete. 6–8 Satisfactory Basic try/catch usage but inconsistent logging. 3–5 Poor Unhandled exceptions or no meaningful logging of errors. 0–2 Criterion 5: Role-Based Access Control (10 marks) Level Description Marks Clear separation between Admin, Inspector, and Viewer roles. Excellent 9–10 Server-side authorization correctly enforced. Good Roles implemented but minor access-control gaps. 6–8 Satisfactory Basic roles present but weak enforcement. 3–5 Poor No meaningful authorization rules. 0–2 Criterion 6: GitHub & CI Workflow (10 marks)\r\nLevel Description Marks CI workflow builds and tests successfully in Release Excellent 9–10 configuration. Repository cleanly structured. Good CI present but minor issues. 6–8 Satisfactory Partial CI or inconsistent repository structure. 3–5 Poor Missing or failing CI workflow. 0–2',NULL,'2026-03-22 15:08:22','2026-03-22 15:44:53');
/*!40000 ALTER TABLE `assignments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `password_reset_tokens`
--

DROP TABLE IF EXISTS `password_reset_tokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `password_reset_tokens` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `code` varchar(6) NOT NULL,
  `expires_at` datetime NOT NULL,
  `used` tinyint(1) NOT NULL DEFAULT '0',
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `idx_user` (`user_id`),
  CONSTRAINT `fk_password_reset_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `password_reset_tokens`
--

LOCK TABLES `password_reset_tokens` WRITE;
/*!40000 ALTER TABLE `password_reset_tokens` DISABLE KEYS */;
INSERT INTO `password_reset_tokens` VALUES (1,2,'856533','2026-03-22 09:28:18',0,'2026-03-22 14:13:17'),(2,2,'179455','2026-03-22 09:29:07',1,'2026-03-22 14:14:07');
/*!40000 ALTER TABLE `password_reset_tokens` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pending_registrations`
--

DROP TABLE IF EXISTS `pending_registrations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pending_registrations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `email` varchar(255) NOT NULL,
  `full_name` varchar(255) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `verification_code` varchar(6) NOT NULL,
  `expires_at` datetime NOT NULL,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_pending_email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pending_registrations`
--

LOCK TABLES `pending_registrations` WRITE;
/*!40000 ALTER TABLE `pending_registrations` DISABLE KEYS */;
/*!40000 ALTER TABLE `pending_registrations` ENABLE KEYS */;
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
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `routine_slots`
--

LOCK TABLES `routine_slots` WRITE;
/*!40000 ALTER TABLE `routine_slots` DISABLE KEYS */;
INSERT INTO `routine_slots` VALUES (2,1,0,'09:00:00','22:00:00','job','','2026-03-08 22:17:32','2026-03-08 22:17:32'),(3,1,2,'09:00:00','22:00:00','job','','2026-03-08 22:17:41','2026-03-08 22:17:41'),(4,1,3,'09:00:00','22:00:00','job','','2026-03-08 22:17:49','2026-03-08 22:17:49'),(5,1,4,'09:00:00','22:00:00','job','','2026-03-08 22:17:57','2026-03-08 22:17:57'),(6,1,5,'09:00:00','22:00:00','job','','2026-03-08 22:18:06','2026-03-08 22:18:06'),(7,1,6,'09:00:00','22:00:00','job','','2026-03-08 22:18:14','2026-03-08 22:18:14'),(8,1,1,'09:00:00','22:00:00','job','','2026-03-15 03:45:36','2026-03-15 03:45:36');
/*!40000 ALTER TABLE `routine_slots` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `stress_logs`
--

DROP TABLE IF EXISTS `stress_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `stress_logs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `log_date` date NOT NULL,
  `stress_level` tinyint NOT NULL COMMENT '1=low to 5=high',
  `note` text,
  `created_at` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_user_date` (`user_id`,`log_date`),
  KEY `idx_user_date` (`user_id`,`log_date`),
  CONSTRAINT `fk_stress_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `stress_logs`
--

LOCK TABLES `stress_logs` WRITE;
/*!40000 ALTER TABLE `stress_logs` DISABLE KEYS */;
INSERT INTO `stress_logs` VALUES (1,1,'2026-03-15',5,'','2026-03-15 06:49:37');
/*!40000 ALTER TABLE `stress_logs` ENABLE KEYS */;
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
  `email_verified` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'test@test.com','$2a$12$BTVbvn1Gf.LfuZvf/NhKPegPq/hKtemgCRIBEB7EdtmsaHAS6xDjS','tet','2026-03-08 20:38:40','2026-03-08 20:38:40',1),(2,'muhammad.taimoor525@gmail.com','$2a$12$ltMTKC1rld8kcVDjY1l1a.vD32hAol3hHASed0qXVBK7ieowCUTSS','test','2026-03-22 14:13:06','2026-03-22 14:14:35',1);
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

-- Dump completed on 2026-03-22 15:47:24
