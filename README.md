# 🎓 StudentJobFinder

**StudentJobFinder** is an intelligent recruitment platform designed to bridge the gap between academic success and professional opportunities. By leveraging the **OpenAI API**, the system analyzes university transcripts to infer technical skills and calculate objective competency levels, ensuring students are matched with roles that truly fit their background.

---

## 🚀 Core Functionality

### 1. AI-Driven Skill Inference
The application takes raw course data from a student's transcript and uses **LLM-based categorization** to map academic history to industry-standard IT skills.
> **Example:** The course *"Web Application Design"* is automatically mapped to the **Web Development** skill category.

![Skill Inference Dashboard](Student%20Job%20Finder/ReadMeImages/skill-inference.png)

### 2. Competency Scoring Engine
Rather than just identifying a skill, the system calculates a **Proficiency Score**. This is derived from a weighted average of the grades achieved in all courses associated with a specific skill, providing recruiters with a data-backed metric of a student's theoretical foundation.

### 3. Recruitment & Validation
*   **Job Matching:** Students receive a "Match Score" for job postings based on the overlap between their inferred skills and the job's requirements.

![Job Matching Card](Student%20Job%20Finder/ReadMeImages/job-matching.png)

*   **Verification Quizzes:** Recruiters can attach custom assessment quizzes to job posts to verify skills or supplement academic results.
*   **Two-Way Feedback:** Includes a feedback system linked to recruiters, allowing students to share insights into the workplace culture.
*   **Applications View:** Recruiters can see all applications for their job posts, including match scores and student info.  
  > If a job post has a quiz attached, the **match score is calculated based on the quiz results**, not the inferred skill levels of the student profiles. The quiz takes precedence, ensuring that verified competencies drive the match.

![Applications View](Student%20Job%20Finder/ReadMeImages/applications-view.png)

---

## 🛠 Technical Stack

*   **Framework:** `.NET 8` (ASP.NET Core Web API)
*   **Data Access:** `Dapper` (Micro-ORM)
*   **Database:** `SQL Server`
*   **AI Integration:** `OpenAI API` for semantic analysis of academic data

---

## ⚙️ Installation & Setup

### Prerequisites
*   .NET 8 SDK
*   SQL Server
*   OpenAI API Key

### Step-by-Step Setup
1. **Clone the repository**

2. **Configure Environment Variables**
   * Locate `appsettings.Example.json` in the root directory.
   * Duplicate the file and rename it to `appsettings.json`.
   * Update the `ConnectionStrings` and `OpenAI_Key` values with your local credentials.

3. **Database Setup**
   > **Note:** The current version requires manual schema creation based on the entities found in the `/Models` directory.  
   > *(SQL migration scripts coming soon.)*

4. **Run the Application**
