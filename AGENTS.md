# Project Overview: Dynamic Engineering Squad (DES) Agents

This document serves as a comprehensive guide for agents and developers working on the **Dynamic Engineering Squad** senior capstone project. It outlines the architecture, coding standards, core features, and the team's specific "way of working."

## 1. Vision & Project Scope
DES is a gamified infrastructure reporting application designed to transform residents into active monitors.
*   **Status:** Senior Capstone Project (Ends at graduation).
*   **Focus:** Proof of Concept (PoC) with a high priority on functional backend logic over frontend aesthetics (current phase).
*   **Goal:** Increase municipal accountability through social engagement and high-visibility reporting.

---

## 2. Technical Stack
*   **Framework:** ASP.NET Core 8.0+ (MVC)
*   **Language:** C#
*   **Database:** SQL Server (EF Core)
*   **Frontend:** HTML5, CSS3, Vanilla JavaScript, Bootstrap 5
*   **APIs:** Google Maps (Geocoding & Maps), OpenAI (Moderation)
*   **Testing:** NUnit (Unit/Integration), Jest (JS), Selenium (UI)
*   **CI/CD:** GitHub Actions

---

## 3. Team Workflow & Collaboration
*   **Agile Cadence:** 2-week sprints with formal retrospectives.
*   **Task Management:** Jira for feature/task tracking; Discord for informal, high-frequency communication.
*   **Branching Strategy:** **Forking Workflow with GitFlow.** Members fork the `upstream` repo and create feature branches off their local `dev` (`upstream/dev` -> `origin/dev` -> `origin/feature/*`).
*   **PR Policy:** Completed features are submitted via Pull Request from the member's fork to the `upstream/dev` branch. Every PR must pass the CI gatekeeper before merging. Merge conflicts are resolved by the feature owner.
*   **Knowledge Sharing:** Rely on high-quality code comments, documentation, and direct code reviews.
*   **Decision Making:** Architectural decisions are usually a 4-way vote; final implementation calls are made by an informal "lead."

---

## 4. Engineering Standards
*   **Error Handling:** **"Fail Gracefully."** Prioritize user-friendly feedback and system stability over raw exceptions.
*   **Development Focus:** **Backend-First.** Ensuring business logic, API integrations, and data integrity are solid before finalizing UI/UX consistency.
*   **AI Collaboration:** Mandatory usage of CLI AI agents (e.g., Gemini CLI, Claude Code, OpenAI Codex) for feature implementation, specifically during "AI Sprints."
*   **Naming:** PascalCase for classes/methods, camelCase for local variables/private fields (prefixed with `_`).
*   **Safety:** Always use `Html.AntiForgeryToken()` and validate inputs to prevent XSS/SQLi.

---

## 5. Architecture & Design Patterns
The project follows a modular, layered architecture:
1.  **Presentation (MVC):** Controllers handle requests and return Views or JSON.
2.  **Service Layer:** Encapsulates business logic (e.g., `ReportIssueService`, `ContentModerationService`).
3.  **Data Access (Repository Pattern):** Decouples EF Core from services using interfaces (e.g., `IReportIssueRepository`).
4.  **Models/DTOs/ViewModels:**
    *   **Models:** EF Core entities (e.g., `ReportIssue`, `UserPoints`).
    *   **DTOs:** Data Transfer Objects for API responses (e.g., `NearbyIssueDTO`).
    *   **ViewModels:** Data structures for rendering Views (e.g., `LatestReportsViewModel`).

---

## 6. Core Features Implementation

### A. AI-Driven Moderation (`ContentModerationService`)
A two-layer moderation approach:
1.  **Local Blocklist:** Fast, regex-based check against a `badWords.txt` file (includes leetspeak normalization).
2.  **OpenAI Fallback:** If the local check passes, the text is sent to OpenAI's `omni-moderation-latest` model.
3.  **Fail-Closed Policy:** If moderation fails (network error), reports are set to "Pending" until reviewed.

### B. Duplicate Image Detection (`ImageHashService`)
*   **Exact Duplicates:** SHA-256 hash comparison.
*   **Visual Similarity:** Perceptual Hashing (pHash) with Hamming Distance calculation (threshold of 8 bits).

### C. Gamification (`LeaderboardService`)
*   **UserPoints:** Tracks `CurrentPoints` and `LifetimePoints`.
*   **Point Awarding:** Users earn 10 points for every moderated report submission.

### D. Audit Logging (`AuditLogService`)
*   **Separate Feature:** Audit logging is a standalone admin feature and must remain completely separate from `ModerationActionLogs`.
*   **Server-Side Only:** Audit logs are written only by server code. Never accept audit log fields from form posts, JavaScript, or other client input.
*   **Append-Only:** Audit logs are read-only after creation. Normal users cannot view, edit, delete, or otherwise manage them.
*   **Captured Fields:** Each log entry stores `Id`, `AspNetUserId`, `UserName`, `Email`, `Role`, `TimestampUtc`, and `Action`.
*   **Best-Effort Logging:** Logging failures must be caught and written through `ILogger`; audit logging must never block or break the main workflow.
*   **Current Logged Actions:** Registration, login, user role changes, minigame play results, issue flagging, report submission, report verification, points shop purchases, and avatar changes.
*   **Admin Access:** Audit logs are exposed through an admin-only read-only page that shows newest entries first and is limited to the latest 100 records.

---

## 7. Testing Strategy
*   **Unit Tests (`InfrastructureApp_Tests`):** Focused on service logic and repository queries.
*   **Frontend Tests (`InfrastructureApp_TestsJS`):** Uses Jest for JS behaviors (maps, autocomplete).
*   **Integration Tests:** Verify DB interactions and external API mockups.

---

## 8. Safety & DevOps
*   **Secrets Management:** **NEVER** commit API keys or connection strings. Use `dotnet user-secrets` for local development (OpenAI, Google Maps, etc.).
*   **Database Migrations:** Use **EF Core Migrations** for all schema changes. Ensure migrations are generated and tested locally before inclusion in a PR.
*   **Verification Workflow:** 
    1.  Develop and test locally.
    2.  Ensure all NUnit and Jest tests pass.
    3.  Verify via CI gatekeeper on the `upstream/dev` PR.
    *   *Note:* There is no staging environment; local and CI verification are the final gates.

---

## 9. Temp Files
* Do not create temp, build, or scratch folders in the project root.
* All generated files must go into appropriate directories:
    1. wwwroot/js for scripts
    2. wwwroot/css for styles
    3. Views for Razor pages
    4. Services/Controllers for backend logic

