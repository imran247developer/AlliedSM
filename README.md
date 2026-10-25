# 🏫 Allied School Management System (SMS)

A comprehensive, enterprise-grade **School Management System** built with **ASP.NET Core Web API** (Backend) and **Angular** (Frontend). Designed to streamline administrative tasks, manage student/staff records, handle fee challan generations, track attendance, manage exam results, and send automated SMS notifications.

---

## 🚀 Features & Modules

### 👨‍💼 1. Admin Module
* User Role Management (Admin, Teacher, Student, Accountant).
* System settings & academic session configurations.
* Overall analytics dashboard.

### 🎓 2. Student Module
* Student registration, admission, and profile management.
* Section & Class assignments.
* Guardian/Parent details tracking.

### 👨‍🏫 3. Teacher & Employee Module
* Staff onboarding and profile management.
* Salary, designation, and subject allocation management.

### 📝 4. Attendance System
* Daily student & staff attendance marking.
* Attendance history, monthly summaries, and reports.

### 🧾 5. Accounts & Fee Management
* Automated Monthly Fee Challan Generation.
* Fee collection, payment status tracking (Paid/Unpaid), and receipts.

### 📊 6. Examinations & Results
* Exam scheduling and subject-wise mark entry.
* Student report card / result card generation.

### 📱 7. SMS Notification System
* Automated SMS alerts for attendance (absent/late notifications).
* Fee challan payment reminders and result updates sent to parents' mobile numbers via SMS Gateway integration.

---

## 🛠️ Tech Stack

### **Backend**
* **Framework:** ASP.NET Core 8.0 / 9.0 Web API
* **Database:** Microsoft SQL Server
* **ORM:** Entity Framework Core (Code-First Approach)
* **Authentication:** ASP.NET Core Identity + JWT (JSON Web Tokens)

### **Frontend**
* **Framework:** Angular (v17+)
* **Styling & UI:** Bootstrap / Angular Material / Tailwind CSS
* **HTTP Client:** RxJS & Angular HttpClient

---

## 📐 Database Architecture (Code-First)

The database models are designed using EF Core Code-First approach:
* `ApplicationUser` (Identity extension)
* `Student` & `Teacher`
* `Attendance`
* `FeeChallan`
* `ExamResult`

---

## ⚡ Getting Started (Local Setup)

### Prerequisites
* [Visual Studio 2022](https://visualstudio.microsoft.com/)
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
* [SQL Server & SSMS](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
* [Node.js & npm](https://nodejs.org/)
* [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)

---

### 🔧 1. Backend Setup (ASP.NET Core Web API)

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/your-username/allied-school-management-system.git](https://github.com/your-username/allied-school-management-system.git)
   cd allied-school-management-system/Backend
