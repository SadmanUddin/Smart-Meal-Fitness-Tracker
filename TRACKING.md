# Smart Meal & Fitness Tracker — Project Tracking

> **Purpose:** This file is maintained after every significant change to give any developer a clear, up-to-date picture of what the app does, how it is structured, what has been done, and what still needs to be done.
>
> **Last updated:** 2026-04-17

---

## Table of Contents
1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
3. [Solution Structure](#3-solution-structure)
4. [Database Schema](#4-database-schema)
5. [Model ↔ Table Mapping](#5-model--table-mapping)
6. [Service Layer Summary](#6-service-layer-summary)
7. [User Journey & View-by-View Detail](#7-user-journey--view-by-view-detail)
8. [Data Flow: User Input → Database](#8-data-flow-user-input--database)
9. [What Is and Is Not Persisted](#9-what-is-and-is-not-persisted)
10. [Git History](#10-git-history)
11. [Change Log (Session-by-Session)](#11-change-log-session-by-session)
12. [Known Remaining Issues](#12-known-remaining-issues)
13. [Architecture Notes](#13-architecture-notes)

---

## 1. Project Overview

**Smart Meal & Fitness Tracker** is a Windows desktop application (WPF, .NET 8) that allows users to:

- Register and log in securely via Supabase Auth
- Browse a public food database and log meals by grams and meal type
- Track physical activities (calories burned, duration)
- Set a daily calorie goal
- View a real-time dashboard summarising today's intake, activities, and calorie balance
- Review full meal history and delete individual entries

The backend is **Supabase** (hosted PostgreSQL + Auth). The app communicates with it via the official Supabase C# SDK using a PostgREST ORM pattern.

---

## 2. Technology Stack

| Layer | Technology |
|---|---|
| UI | WPF (Windows Presentation Foundation) |
| Language | C# 12 / .NET 8.0 |
| Backend | Supabase (PostgreSQL + Auth) |
| ORM | Supabase Postgrest C# client (`Supabase` NuGet v1.1.1) |
| Testing | xUnit v2.5.3 (scaffolded only — no tests written yet) |
| Config | `supabase.config.json` (loaded at startup, must not be committed) |

---

## 3. Solution Structure

```
SmartMealSolution/
├── SmartMeal/                  ← WPF app (UI layer)
│   ├── MainWindow.xaml/.cs     ← App shell, service initialization, navigation
│   ├── Views/
│   │   ├── LoginView.xaml/.cs
│   │   ├── RegisterView.xaml/.cs
│   │   ├── DashboardView.xaml/.cs
│   │   ├── AddMealView.xaml/.cs
│   │   ├── AddActivityView.xaml/.cs
│   │   ├── SetGoalView.xaml/.cs
│   │   └── MealsView.xaml/.cs
│   └── supabase.config.json    ← Credentials (gitignored — never commit)
│
├── SmartMeal.core/             ← Business logic layer
│   ├── Models/
│   │   ├── User.cs             ← Maps to `users` table
│   │   ├── FoodItem.cs         ← Maps to `food_items` table
│   │   ├── Meal.cs (MealLog)   ← Maps to `meal_logs` table
│   │   ├── MealType.cs         ← Maps to `meal_types` table
│   │   ├── Goal.cs             ← Maps to `goals` table
│   │   ├── WeightLog.cs        ← Maps to `weight_logs` table
│   │   └── Activity.cs         ← Maps to `activities` table
│   └── Services/
│       ├── AuthService.cs
│       ├── MealService.cs
│       ├── FoodService.cs
│       ├── GoalService.cs
│       ├── WeightLogService.cs
│       └── ActService.cs
│
├── SmartMeal.Data/             ← Data access layer
│   ├── Context/
│   │   └── SupabaseClientProvider.cs
│   └── Repositories/
│       └── UserRepository.cs   ← Empty stub, not used
│
└── SmartUnit.Tests/            ← Unit tests (empty — no coverage yet)
```

---

## 4. Database Schema

Hosted on Supabase (PostgreSQL). All tables live in the `public` schema.

### `users`
| Column | Type | Notes |
|---|---|---|
| id | uuid PK | Set from Supabase Auth user ID on register |
| full_name | text | Required |
| email | text UNIQUE | Required |
| role | text | `'user'` or `'admin'`, default `'user'` |
| age | int | Nullable |
| height_cm | numeric(5,2) | Nullable |
| weight_kg | numeric(5,2) | Nullable |
| gender | text | Nullable, constrained to `male/female/other` |
| created_at | timestamptz | Default `now()` |

### `food_categories`
| Column | Type | Notes |
|---|---|---|
| food_category_id | smallint PK | Auto-generated identity |
| name | text UNIQUE | e.g. Protein, Carbs, Vegetables |
| display_order | smallint UNIQUE | Controls dropdown sort order |

Seeded with 17 categories (Protein, Carbs, Vegetables, Fruits, Dairy, etc.)

### `food_items`
| Column | Type | Notes |
|---|---|---|
| food_id | bigint PK | Auto-generated identity |
| name | text | Case-insensitive unique index for public items |
| food_category_id | smallint FK | References `food_categories` |
| calories_per_100g | numeric(8,2) | ≥ 0 |
| protein_per_100g | numeric(8,2) | ≥ 0, default 0 |
| carbs_per_100g | numeric(8,2) | ≥ 0, default 0 |
| fats_per_100g | numeric(8,2) | ≥ 0, default 0 |
| created_by_user_id | uuid FK | NULL for public items, required for private |
| is_public | boolean | Default true |
| is_active | boolean | Default true |
| created_at | timestamptz | Default `now()` |

Constraint: `(is_public=true AND created_by_user_id IS NULL) OR (is_public=false AND created_by_user_id IS NOT NULL)`

Seeded with 44 public food items.

### `meal_types`
| Column | Type | Notes |
|---|---|---|
| meal_type_id | smallint PK | Auto-generated identity |
| name | text UNIQUE | `breakfast`, `lunch`, `dinner`, `snack` |
| display_order | smallint UNIQUE | Controls dropdown sort order |

### `meal_logs`
| Column | Type | Notes |
|---|---|---|
| meal_log_id | bigint PK | Auto-generated identity |
| user_id | uuid FK | References `users(id)` ON DELETE CASCADE |
| food_id | bigint FK | References `food_items(food_id)` ON DELETE RESTRICT |
| grams | numeric(8,2) | > 0 |
| meal_type_id | smallint FK | References `meal_types` ON DELETE RESTRICT |
| log_date | date | Default `current_date` |
| created_at | timestamptz | Default `now()` |

### `goals`
| Column | Type | Notes |
|---|---|---|
| goal_id | bigint PK | Auto-generated identity |
| user_id | uuid FK UNIQUE | One goal row per user |
| target_weight_kg | numeric(5,2) | Nullable |
| calorie_goal | numeric(8,2) | Nullable, ≥ 0 |
| protein_goal | numeric(8,2) | Nullable |
| carbs_goal | numeric(8,2) | Nullable |
| fat_goal | numeric(8,2) | Nullable |
| created_at | timestamptz | Default `now()` |

### `weight_logs`
| Column | Type | Notes |
|---|---|---|
| weight_log_id | bigint PK | Auto-generated identity |
| user_id | uuid FK | References `users(id)` ON DELETE CASCADE |
| weight_kg | numeric(5,2) | > 0 |
| logged_at | timestamptz | Default `now()` |
| notes | text | Nullable |

---

## 5. Model ↔ Table Mapping

| C# Model | DB Table | Status |
|---|---|---|
| `User.cs` | `users` | ✅ Fully mapped |
| `FoodItem.cs` | `food_items` | ⚠️ Missing `created_by_user_id` column (read-only use unaffected) |
| `MealLog` (Meal.cs) | `meal_logs` | ✅ Fully mapped |
| `MealType.cs` | `meal_types` | ✅ Fully mapped |
| `Goal.cs` | `goals` | ✅ Fully mapped and in active use |
| `WeightLog.cs` | `weight_logs` | ✅ Fully mapped — no UI yet |
| `Activity.cs` | `activities` | ✅ Fully mapped and persisted via Supabase |
| *(none)* | `food_categories` | No C# model. `FoodItem.FoodCategoryId` stores the FK but category name is never fetched. |

---

## 6. Service Layer Summary

### `AuthService` — `SmartMeal.core/Services/AuthService.cs`
- `RegisterAsync(name, email, password, confirmPassword)` — creates Supabase Auth account + inserts `users` row
- `LoginAsync(email, password)` — signs in, loads `users` row into `CurrentUser`
- `SignOutAsync()` — signs out of Supabase Auth, clears `CurrentUser`
- `CurrentUser` (property) — the currently logged-in `User` model; `null` when logged out

### `MealService` — `SmartMeal.core/Services/MealService.cs`
- `AddMealLogAsync(userId, foodId, grams, mealTypeId)` — inserts a `meal_logs` row
- `GetTodayLogsAsync(userId)` — fetches today's logs for the user (filtered by `log_date`)
- `GetAllLogsAsync(userId)` — fetches all logs for the user, ordered by `log_date DESC`
- `DeleteMealLogAsync(mealLogId)` — deletes a single `meal_logs` row by primary key

### `FoodService` — `SmartMeal.core/Services/FoodService.cs`
- `GetPublicFoodsAsync()` — returns all active public food items, A→Z
- `GetMealTypesAsync()` — returns all meal types ordered by `display_order`

### `GoalService` — `SmartMeal.core/Services/GoalService.cs`
- `UpsertGoalAsync(userId, calorieGoal)` — inserts or updates the user's goal row in `goals`
- `GetGoalAsync(userId)` — returns the user's `Goal` or `null` if none set
- **Backed by Supabase** — goals persist across sessions

### `ActService` — `SmartMeal.core/Services/ActService.cs`
- `AddActivityAsync(activity)` — inserts a row into `activities`
- `GetActivitiesByUserAsync(userId)` — fetches user-scoped activity rows ordered by `logged_at`
- **Backed by Supabase** — activities persist across sessions and are isolated per user

### `WeightLogService` — `SmartMeal.core/Services/WeightLogService.cs`
- `AddWeightLogAsync(userId, weightKg, notes)` — inserts a `weight_logs` row
- `GetWeightLogsByUserAsync(userId)` — fetches all weight logs for a user (`logged_at DESC`)
- `GetLatestWeightLogAsync(userId)` — returns the newest log or `null`
- `UpdateWeightLogAsync(weightLogId, weightKg, notes)` — updates weight/notes by primary key
- `DeleteWeightLogAsync(weightLogId)` — deletes a single `weight_logs` row by primary key
- **Backed by Supabase** — service is ready, UI still not implemented

---

## 7. User Journey & View-by-View Detail

This section covers every view in the order a new user would encounter them, including: what they see, what inputs are available, what validation runs, what DB operations happen, and what happens on success or failure.

---

### App Startup

**File:** `MainWindow.xaml.cs` → `InitializeAsync()`

On launch the app does the following before showing any UI:

1. Reads `supabase.config.json` from `bin/Debug/net8.0-windows/`
2. Validates the URL and anon key are not placeholders
3. Calls `SupabaseClientProvider.InitializeAsync()` — opens the Supabase connection
4. Creates all services: `AuthService`, `MealService`, `FoodService`, `GoalService`, `WeightLogService`, `ActService`
5. Navigates to `RegisterView`

If any step fails (missing file, bad credentials, no network) a modal error is shown and the app closes.

---

### View 1: RegisterView

**File:** `SmartMeal/Views/RegisterView.xaml` / `.cs`

**Purpose:** Create a new account.

#### User Inputs
| Control (x:Name) | Type | What the user enters |
|---|---|---|
| `FullNameTextBox` | TextBox | Their display name (e.g. "Jane Smith") |
| `EmailTextBox` | TextBox | A valid email address |
| `PasswordBox` | PasswordBox | Password (min 6 characters) |
| `ConfirmPasswordBox` | PasswordBox | Must match Password |

#### Validation (in `AuthService.RegisterAsync`)
| Rule | Error shown if violated |
|---|---|
| Any field empty | "Please fill in all fields." |
| Email missing `@` or `.` | "Invalid email format." |
| Passwords don't match | "Passwords do not match." |
| Password shorter than 6 chars | "Password must be at least 6 characters." |

#### DB Operations on Success
1. **Supabase Auth** — `_client.Auth.SignUp(email, password)` creates an auth account. Returns a session with a UUID user ID.
2. **`users` table** — Inserts a new row:
   ```
   id           = session.User.Id   (UUID string from Supabase Auth)
   full_name    = FullNameTextBox.Text
   email        = EmailTextBox.Text
   role         = "user"
   created_at   = DateTime.UtcNow
   age, height_cm, weight_kg, gender = NULL (not collected at registration)
   ```

#### What happens next
- **Success:** MessageBox "Registration successful! Please log in." → navigates to `LoginView`
- **Failure:** MessageBox shows the error. User stays on RegisterView.

#### Navigation links
- "Already have an account? Login" → navigates to `LoginView`

---

### View 2: LoginView

**File:** `SmartMeal/Views/LoginView.xaml` / `.cs`

**Purpose:** Sign into an existing account.

#### User Inputs
| Control (x:Name) | Type | What the user enters |
|---|---|---|
| `EmailTextBox` | TextBox | Registered email address |
| `PasswordBox` | PasswordBox | Account password |

#### Validation (in `AuthService.LoginAsync`)
| Rule | Error shown if violated |
|---|---|
| Either field empty | "Please fill in all fields." |

#### DB Operations on Success
1. **Supabase Auth** — `_client.Auth.SignIn(email, password)` — authenticates and returns a session
2. **`users` table** — SELECT where `id = session.User.Id` → loads the `User` row into `AuthService.CurrentUser`

`AuthService.CurrentUser` is the single source of truth for the logged-in user's identity throughout the rest of the session. Every view reads the user ID from `_authService.CurrentUser?.Id`.

#### What happens next
- **Success:** navigates to `DashboardView`
- **Failure:** MessageBox shows the error message from Supabase (e.g. "Invalid login credentials"). User stays on LoginView.

#### Navigation links
- "Don't have an account? Register" → navigates to `RegisterView`

---

### View 3: DashboardView

**File:** `SmartMeal/Views/DashboardView.xaml` / `.cs`

**Purpose:** Main screen. Shows today's summary and provides quick navigation to all actions.

#### What loads on entry (`LoadDashboardAsync`)

All data loads asynchronously when the view is shown. If the user ID is not available (not logged in), all cards display `0`.

| Card label | DB query | Displayed value |
|---|---|---|
| **Calories Consumed** | `meal_logs` WHERE `user_id = ? AND log_date = today` + `food_items` lookup | Sum of `(grams / 100) * calories_per_100g`, rounded |
| **Meals** | Same query as above | Count of rows returned |
| **Activities** | `activities` WHERE `user_id = ?` | Count of rows returned |
| **Calories Burned** | `activities` WHERE `user_id = ?` | Sum of `calories_burned` across user activities |
| **Balance** | Computed | `calorie_goal − calories_consumed + calories_burned` |
| **Daily Calories Goal** | `goals` WHERE `user_id = ?` | `calorie_goal` from the user's goal row, or `0` if no goal set |

#### Recent Activity panel
| Section | Source | Shown |
|---|---|---|
| Recent meal | Last item from today's `meal_logs` query | `"Food ID {id} — {grams}g"` |
| Recent activity | Last item from user activity query | `"{Name} - {CaloriesBurned} cal burned"` |

> **Note for developers:** The "Recent meal" currently shows the raw `FoodId` integer (e.g. `"Food ID 3 — 150g"`), not the food name. Resolving it to a name would require an additional `food_items` query or joining at the service level. This is a known issue — see Section 12.

#### User Actions (buttons)
| Button | Navigates to |
|---|---|
| Add Meal | `AddMealView` |
| Add Activity | `AddActivityView` |
| Set Goal | `SetGoalView` |
| Meals (sidebar) | `MealsView` |
| Log Out | Confirmation modal → `AuthService.SignOutAsync()` → `LoginView` |

#### Logout flow
1. MessageBox: "Are you sure you want to logout?" Yes/No
2. If Yes: `await _mainWindow.AuthService.SignOutAsync()` — clears `CurrentUser`, signs out of Supabase Auth
3. Navigates to `LoginView`

---

### View 4: AddMealView

**File:** `SmartMeal/Views/AddMealView.xaml` / `.cs`

**Purpose:** Log a meal — pick a food from the database, enter how many grams, choose meal type.

#### What loads on entry (`LoadDropdownsAsync`)
Two DB queries run when the view opens:
1. `FoodService.GetPublicFoodsAsync()` → populates `FoodComboBox` with all active public foods (A→Z)
   - Each item displays its `Name` property
   - The dropdown is searchable (`IsEditable="True"`, `TextSearch.TextPath="Name"`)
2. `FoodService.GetMealTypesAsync()` → populates `MealTypeComboBox` with meal types (by `display_order`)
   - Shows: Breakfast, Lunch, Dinner, Snack

#### User Inputs
| Control (x:Name) | Type | What the user enters |
|---|---|---|
| `FoodComboBox` | ComboBox (searchable) | Select a food item from the seeded database. Can type to filter. |
| `GramsTextBox` | TextBox | How many grams of that food they ate (numeric, > 0) |
| `MealTypeComboBox` | ComboBox | Which meal this belongs to: Breakfast / Lunch / Dinner / Snack |

#### Validation (in `AddMeal_Click`)
| Rule | Error shown if violated |
|---|---|
| No food selected | "Please select a food item." |
| No meal type selected | "Please select a meal type." |
| Grams is not a valid positive decimal | "Please enter a valid amount of grams." |
| User not logged in (`CurrentUser` null) | "Session expired. Please log in again." → navigates to LoginView |

#### DB Operation on Save
Calls `MealService.AddMealLogAsync(userId, food.FoodId, grams, mealType.MealTypeId)` which inserts into `meal_logs`:
```
meal_log_id   = AUTO (generated by DB)
user_id       = AuthService.CurrentUser.Id
food_id       = selected FoodItem.FoodId
grams         = decimal from GramsTextBox
meal_type_id  = selected MealType.MealTypeId
log_date      = today's date (yyyy-MM-dd, set in C#)
created_at    = DateTime.UtcNow (set in C#, also defaulted by DB)
```

#### What happens next
- **Success:** MessageBox "Meal logged successfully!" → navigates to `DashboardView` (which reloads from DB)
- **Failure:** MessageBox shows the Supabase error. User stays on AddMealView.
- **Cancel button:** navigates back to `DashboardView` without saving

---

### View 5: AddActivityView

**File:** `SmartMeal/Views/AddActivityView.xaml` / `.cs`

**Purpose:** Log a physical activity for the current user.

> **Important:** Activities are persisted in `public.activities` and loaded per user. Data survives app restarts.

#### User Inputs
| Control (x:Name) | Type | What the user enters |
|---|---|---|
| `ActivityNameTextBox` | TextBox | Name of the activity (e.g. "Running", "Gym") |
| `CaloriesBurnedTextBox` | TextBox | Estimated calories burned (integer, ≥ 0) |
| `DurationTextBox` | TextBox | Duration in minutes (integer, > 0) |

#### Validation (in `AddActivity_Click`)
| Rule | Error shown if violated |
|---|---|
| Activity name is blank | "Please enter an activity name." |
| Calories burned is not a valid integer ≥ 0 | "Please enter a valid number for calories burned." |
| Duration is not a valid integer > 0 | "Please enter a valid number for duration." |
| User not logged in | "No user logged in." |

#### What gets stored (`activities` row)
```
UserId         = AuthService.CurrentUser.Id
Name           = ActivityNameTextBox.Text
CaloriesBurned = parsed int
DurationMinutes= parsed int (minutes)
LoggedAt       = DateTime.UtcNow
```

Stored via `await ActService.AddActivityAsync(activity)` → inserted into `public.activities`.

#### What happens next
- **Success:** MessageBox "Activity added successfully!" → navigates to `DashboardView`
- **Cancel button:** navigates back to `DashboardView` without saving

---

### View 6: SetGoalView

**File:** `SmartMeal/Views/SetGoalView.xaml` / `.cs`

**Purpose:** Set (or update) the user's daily calorie target. This persists to the database.

#### User Inputs
| Control (x:Name) | Type | What the user enters |
|---|---|---|
| `DailyGoalTextBox` | TextBox | Target daily calorie intake as a whole number (e.g. 2000) |

#### Validation (in `SetGoal_Click`)
| Rule | Error shown if violated |
|---|---|
| Input is not a valid integer > 0 | "Please enter a valid number for calorie goal." |
| User not logged in | "No user logged in." |

#### DB Operation on Save
Calls `GoalService.UpsertGoalAsync(userId, calorieGoal)`:

1. **Reads** `goals` WHERE `user_id = ?`
2. If a row exists → **UPDATE** `calorie_goal = calorieGoal`
3. If no row exists → **INSERT** new row:
   ```
   goal_id          = AUTO (generated by DB)
   user_id          = AuthService.CurrentUser.Id
   calorie_goal     = parsed integer (stored as decimal)
   target_weight_kg = NULL  (not collected)
   protein_goal     = NULL  (not collected)
   carbs_goal       = NULL  (not collected)
   fat_goal         = NULL  (not collected)
   created_at       = DateTime.UtcNow
   ```

The DB enforces one goal row per user via `UNIQUE(user_id)`. The upsert logic in `GoalService` prevents duplicates at the application level before the DB constraint is reached.

#### What happens next
- **Success:** MessageBox "Goal set successfully!" → navigates to `DashboardView` (CaloriesGoalBlock and Balance update on reload)
- **Failure:** MessageBox shows Supabase error. User stays on SetGoalView.
- **Cancel button:** navigates back to `DashboardView` without saving

---

### View 7: MealsView

**File:** `SmartMeal/Views/MealsView.xaml` / `.cs`

**Purpose:** View the complete history of all logged meals and delete individual entries.

#### What loads on entry (`LoadMealsAsync`)
Calls `MealService.GetAllLogsAsync(userId)` → SELECT from `meal_logs` WHERE `user_id = ?` ORDER BY `log_date DESC`

Populates `MealsDataGrid` with the full history (not just today).

#### DataGrid columns
| Column Header | Bound to | DB source |
|---|---|---|
| Food ID | `MealLog.FoodId` | `meal_logs.food_id` (the FK — raw number, not food name) |
| Grams | `MealLog.Grams` | `meal_logs.grams` |
| Meal Type | `MealLog.MealTypeId` | `meal_logs.meal_type_id` (the FK — raw number, not type name) |
| Date | `MealLog.LogDate` | `meal_logs.log_date` (string "yyyy-MM-dd") |
| Action | Delete button | Tag bound to `MealLog.MealLogId` |

> **Note:** Food ID and Meal Type show raw integer IDs, not human-readable names. This is a known issue — see Section 12.

#### Delete flow
1. User clicks Delete button on a row
2. MessageBox: "Are you sure you want to delete this meal?" Yes/No
3. If Yes: `MealService.DeleteMealLogAsync(mealLogId)` → DELETE from `meal_logs` WHERE `meal_log_id = ?`
4. Grid reloads: `LoadMealsAsync()` runs again

#### User Actions (buttons)
| Button | Action |
|---|---|
| Add Meal | Navigates to `AddMealView` |
| Dashboard (sidebar) | Navigates to `DashboardView` |
| Delete (per row) | Deletes that meal log from DB and refreshes grid |

---

## 8. Data Flow: User Input → Database

This section traces the complete path from user action to database for each write operation.

### Registration
```
User fills form → RegisterButton_Click
  → AuthService.RegisterAsync(name, email, password, confirmPassword)
    → Validate inputs (C# guards)
    → _client.Auth.SignUp(email, password)          [Supabase Auth table]
    → _client.From<User>().Insert(user)             [public.users INSERT]
  → MessageBox + Navigate to LoginView
```

### Login
```
User fills form → LoginButton_Click
  → AuthService.LoginAsync(email, password)
    → Validate inputs
    → _client.Auth.SignIn(email, password)          [Supabase Auth]
    → _client.From<User>().Where(id).Single()       [public.users SELECT]
    → Sets AuthService.CurrentUser
  → Navigate to DashboardView
```

### Add Meal
```
User selects food, enters grams, selects type → AddMeal_Click
  → Validate all 3 inputs
  → MealService.AddMealLogAsync(userId, foodId, grams, mealTypeId)
    → _client.From<MealLog>().Insert(log)           [public.meal_logs INSERT]
  → Navigate to DashboardView (re-queries meal_logs for today)
```

### Delete Meal
```
User clicks Delete on a row → DeleteMeal_Click
  → Confirmation dialog
  → MealService.DeleteMealLogAsync(mealLogId)
    → _client.From<MealLog>().Where(id).Delete()    [public.meal_logs DELETE]
  → LoadMealsAsync() re-queries and refreshes grid
```

### Set Goal
```
User enters calorie number → SetGoal_Click
  → Validate input
  → GoalService.UpsertGoalAsync(userId, calorieGoal)
    → _client.From<Goal>().Where(userId).Get()      [public.goals SELECT]
    → If found:  .Update(existing)                  [public.goals UPDATE]
    → If not:    .Insert(newGoal)                   [public.goals INSERT]
  → Navigate to DashboardView (re-queries goals)
```

### Add Activity
```
User fills form → AddActivity_Click
  → Validate inputs
  → ActService.AddActivityAsync(activity)
    → _client.From<Activity>().Insert(activity)     [public.activities INSERT]
  → Navigate to DashboardView (re-queries user activities from DB)
```

### Logout
```
User clicks Log Out → BackToLog_Click
  → Confirmation dialog
  → AuthService.SignOutAsync()
    → _client.Auth.SignOut()                        [Supabase Auth]
    → CurrentUser = null
  → Navigate to LoginView
```

---

## 9. What Is and Is Not Persisted

Understanding which data survives a restart is critical for knowing what features are complete vs. still in-progress.

| Feature | Persisted to DB | Survives restart | Table |
|---|---|---|---|
| User account | Yes | Yes | `users` |
| Meal logs | Yes | Yes | `meal_logs` |
| Daily calorie goal | Yes | Yes | `goals` |
| Food database | Yes (seeded) | Yes | `food_items`, `food_categories` |
| Meal types | Yes (seeded) | Yes | `meal_types` |
| Activities | Yes | Yes | `activities` |
| Weight logs | Yes (service ready) | Yes | `weight_logs` — no UI |
| Protein/carbs/fat goals | Yes (columns exist) | Yes | `goals` — no UI |
| Target weight goal | Yes (column exists) | Yes | `goals` — no UI |
| User profile (age, height, gender) | Yes (columns exist) | Yes | `users` — no UI to edit after registration |

---

## 10. Git History

| Commit | Message | What it did |
|---|---|---|
| `732383e` | initial Commit | Project scaffolding |
| `8799458` | Login and Register views edited | Basic auth UI |
| `357804f` | Authorization logic updated | Auth service wired up |
| `62d9e84` | Authentication logic and navigation views updated | Login/register flow |
| `6842380` | Dashboard design plus meals count logic updated | Dashboard UI and meal count |
| `2b7dec0` | Activity Feature added | In-memory activity tracking |
| `579aab9` | done with the user calorie goals | In-memory goal service |
| `2fb81c8` | user filtering added | User-scoped data filtering |
| `005d787` | logout added | Logout button on Dashboard |
| `9d587d9` | Meals history upgraded | New `MealsView` with DataGrid |

---

## 11. Change Log (Session-by-Session)

---

### Session 1 — 2026-04-16
**Who:** Developer
**Branch:** `main`

#### A. Pull & Merge — upstream `origin/main` (3 new commits)

Pulled 3 commits from `SadmanUddin/Smart-Meal-Fitness-Tracker`:
- `9d587d9` Meals history upgraded
- `005d787` logout added
- `2fb81c8` user filtering added

New files added by remote:
- `SmartMeal/Views/MealsView.xaml`
- `SmartMeal/Views/MealsView.xaml.cs`

**Merge conflicts resolved in 4 files** (local stash vs upstream):

| File | Conflict | Resolution |
|---|---|---|
| `MainWindow.xaml.cs` | Using statements + service declarations | Kept stashed version (null-safe `= null!` pattern) + retained `User? CurrentUser` property for backward compat |
| `LoginView.xaml.cs` | Upstream bug: navigate-on-success code was inside `!result.Success` block | Kept stashed version: show error + return on failure, navigate on success |
| `AddMealView.xaml.cs` | Old in-memory meal insert vs new Supabase insert | Kept stashed version: proper async insert with try/catch |
| `DashboardView.xaml.cs` | Old sync `LoadMeals()` + old constructor vs new async pattern | Kept stashed version: single async `LoadDashboardAsync()` with `AuthService.CurrentUser` |

---

#### B. Schema vs Code Audit

Full cross-reference of the Supabase DB schema against C# models and services. Identified:

**Compilation errors (app would not build):**
1. `MealService.DeleteMeal(Guid)` — referenced `Meals` static list that does not exist
2. `DashboardView` — called `_goalService.GetGoal()` (no-arg), method signature required `Guid userId`
3. `SetGoalView` — assigned `User.Id` (string) to `FitGoal.UserId` (Guid) — type mismatch
4. `MealsView` — called `mealService.GetMealsByUser()` which does not exist on `MealService`
5. `MealsView.DeleteMeal_Click` — used `Guid` for meal ID; actual PK is `long`

**Logic errors:**
- `GoalService` was entirely in-memory (`FitGoal` list), ignoring the fully-mapped `Goal` model and `goals` DB table
- `Activity.UserId` was `Guid`; `User.Id` from Supabase Auth is always `string`
- `AddActivityView` used `mainWindow.CurrentUser` (never set in the new login flow)
- `BackToLog_Click` (logout) set `mainWindow.CurrentUser = null` but never called `AuthService.SignOutAsync()`
- `MealsView` DataGrid bound to old model properties (`Name`, `Calories`, `Category`, `Date`, `Id`) that don't exist on `MealLog`

---

#### C. Schema Fix — 9 files changed

**`SmartMeal.core/Services/GoalService.cs`** — Full rewrite
- Removed: in-memory `static List<FitGoal>`, `AddGoal(FitGoal)`, `GetGoal(Guid)`
- Added: `GoalService(Client client)` constructor
- Added: `UpsertGoalAsync(string userId, int calorieGoal)` — reads existing row, updates or inserts
- Added: `GetGoalAsync(string userId)` — returns `Goal?` from DB
- Goals now **persist across sessions** in the `goals` table

**`SmartMeal.core/Services/MealService.cs`** — Deleted broken method, added correct one
- Removed: `DeleteMeal(Guid mealID)` — was referencing non-existent `Meals` list
- Added: `DeleteMealLogAsync(long mealLogId)` — deletes from `meal_logs` by PK

**`SmartMeal.core/Models/Activity.cs`** — Type fix
- Changed: `public Guid UserId` → `public string UserId = string.Empty`
- Reason: Supabase Auth user IDs are strings (UUID format), not `System.Guid`

**`SmartMeal/MainWindow.xaml.cs`** — GoalService initialization
- Changed: `GoalService { get; } = new()` → `GoalService { get; private set; } = null!`
- Added: `GoalService = new GoalService(provider.Client)` in `InitializeAsync()`
- Reason: GoalService now requires the Supabase client

**`SmartMeal/Views/SetGoalView.xaml.cs`** — Async + DB-backed
- Removed: `FitGoal` object creation, `goalService.AddGoal(goal)`
- Changed: `SetGoal_Click` to `async void`
- Changed: user ID source from `mainWindow.CurrentUser.Id` → `_authService.CurrentUser?.Id`
- Added: `await _goalService.UpsertGoalAsync(userId, calorieGoal)` with try/catch

**`SmartMeal/Views/DashboardView.xaml.cs`** — Goal loading + proper logout
- Changed: `_goalService.GetGoal()` → `await _goalService.GetGoalAsync(userId)`
- Changed: `(int)(goal?.CalorieGoal ?? 0)` since `Goal.CalorieGoal` is `decimal?`
- Changed: `BackToLog_Click` to `async void`
- Removed: `mainWindow.CurrentUser = null` (redundant, never set)
- Added: `await _mainWindow.AuthService.SignOutAsync()` before navigating to login

**`SmartMeal/Views/AddActivityView.xaml.cs`** — Full rewrite of code-behind
- Added: `_mainWindow`, `_authService` fields (consistent pattern with other views)
- Changed: user ID from `mainWindow.CurrentUser.Id` (Guid mismatch) → `_authService.CurrentUser?.Id`
- Added: `Duration = durationMinutes` to `Activity` initializer (was being collected from the form but silently dropped)
- Added: activity name empty check
- Improved: error messages

**`SmartMeal/Views/MealsView.xaml.cs`** — Full rewrite of code-behind
- Added: `_mainWindow`, `_authService` fields
- Changed: synchronous `LoadMeals()` in constructor → async via `Loaded` event → `LoadMealsAsync()`
- Changed: `mealService.GetMealsByUser(id)` → `await _mealService.GetAllLogsAsync(userId)`
- Changed: `button.Tag is Guid` → `button.Tag is long` (meal PK is `long`)
- Changed: `mealService.DeleteMeal(mealId)` → `await _mealService.DeleteMealLogAsync(mealLogId)`
- Added: try/catch on both load and delete operations

**`SmartMeal/Views/MealsView.xaml`** — DataGrid column bindings
- Updated all column bindings from old in-memory model to `MealLog` properties:

| Old Header | Old Binding | New Header | New Binding |
|---|---|---|---|
| Meal Name | `{Binding Name}` | Food ID | `{Binding FoodId}` |
| Calories | `{Binding Calories}` | Grams | `{Binding Grams}` |
| Category | `{Binding Category}` | Meal Type | `{Binding MealTypeId}` |
| Date | `{Binding Date}` | Date | `{Binding LogDate}` |
| *(delete tag)* | `{Binding Id}` | *(delete tag)* | `{Binding MealLogId}` |

---

#### D. Supabase Config Verification

Confirmed the full configuration chain is intact and ready to use:
- `supabase.config.json` — live credentials present
- `.gitignore` — file is excluded from version control ✅
- `SmartMeal.csproj` — `CopyToOutputDirectory: PreserveNewest` ✅
- `MainWindow.InitializeAsync()` — reads, validates, and connects ✅
- All 4 services initialized with the Supabase client ✅

---

### Session 2 — 2026-04-16
**Who:** Developer
**Branch:** `main`

#### A. Activities moved to Supabase
- `Activity` model mapped to `public.activities` with `[Table]`, `[PrimaryKey]`, and `[Column]` attributes.
- `ActService` rewritten from in-memory list storage to Supabase-backed async methods:
  - `AddActivityAsync(...)`
  - `GetActivitiesByUserAsync(...)`
- `MainWindow` now initializes `ActService` with the shared Supabase client.
- `AddActivityView` now saves to DB asynchronously and handles insert errors.
- `DashboardView` now queries user activities from DB (no cross-user in-process leakage).

#### B. Weight log persistence service added
- Added `SmartMeal.core/Services/WeightLogService.cs` with DB methods:
  - insert (`AddWeightLogAsync`)
  - read list/latest (`GetWeightLogsByUserAsync`, `GetLatestWeightLogAsync`)
  - update (`UpdateWeightLogAsync`)
  - delete (`DeleteWeightLogAsync`)
- `MainWindow` now initializes and exposes `WeightLogService`.
- No UI has been added yet for weight log creation/history.

#### C. Dashboard math correction
- Dashboard now computes consumed calories from meal logs using food nutrition values.
- Balance formula fixed to use consistent units:
  - `balance = calorie_goal - calories_consumed + calories_burned`

---

## 12. Known Remaining Issues

### UI/UX
| Issue | File(s) | Details |
|---|---|---|
| ~~MealsView shows raw FoodId number~~ | ~~MealsView.xaml~~ | ✅ Fixed in Session 3 — `MealViewRow` projection resolves names |
| ~~MealsView shows raw MealTypeId number~~ | ~~MealsView.xaml~~ | ✅ Fixed in Session 3 — `MealViewRow` projection resolves names |
| ~~Dashboard recent meal shows raw ID~~ | ~~DashboardView.xaml.cs~~ | ✅ Fixed in Session 3 — food name resolved via `GetPublicFoodsAsync()` lookup |
| Sidebar navigation polish still incomplete | `AddMealView`, `SetGoalView`, `AddActivityView`, `MealsView`, `DashboardView` | Dashboard/Meals/Activities/History navigation works. Profile remains a "coming soon" stub. |

### Missing Features
| Feature | Status | Notes |
|---|---|---|
| Activity history UI | Partial | Activities are persisted, but there is no dedicated full history/manage screen (only add + dashboard summary/recent). |
| ~~Weight logging UI~~ | ~~Missing~~ | ✅ Implemented in Session 4 — `WeightHistoryView` with line graph and log form. |
| Full goal editing | Partial | Only `calorie_goal` is collected. `protein_goal`, `carbs_goal`, `fat_goal`, `target_weight_kg` columns in `goals` table are unused. |
| User profile editing (post-registration) | Partial | Age, height, weight, gender are now collected at registration. No screen to edit them after the fact. |
| Calorie detail UX | Partial | Dashboard shows total calories consumed, but no per-meal calorie breakdown. |
| Food category display | Missing | No `FoodCategory` C# model. Category names not shown anywhere. |

### Technical Debt
| Issue | File(s) | Details |
|---|---|---|
| `UserRepository.cs` is empty | `SmartMeal.Data/Repositories/UserRepository.cs` | Stub class, does nothing. |
| `FoodItem` missing `created_by_user_id` | `SmartMeal.core/Models/FoodItem.cs` | Read-only use is fine now, but private food creation would need this. |
| Zero test coverage | `SmartUnit.Tests/` | Only a scaffolded empty `Test1()` method. No actual tests. |
| No MVVM | All views | Views contain all business logic. Hard to unit test. Consider refactoring to MVVM + commands. |
| No DI container | `MainWindow.xaml.cs` | Services manually instantiated. Consider `Microsoft.Extensions.DependencyInjection`. |

---

### Session 3 — 2026-04-17
**Who:** Developer
**Branch:** `main`

#### A. Human-readable comments added to entire codebase

Every C# file now has guiding comments written for a new developer — not just inline code notes, but top-of-file explanations covering what the class does, why design decisions were made, and how each method fits into the larger flow.

Files commented:

| File | What was explained |
|---|---|
| `SmartMeal.core/Models/User.cs` | `[PrimaryKey]` second param, optional profile fields |
| `SmartMeal.core/Models/FoodItem.cs` | Per-100g nutrition convention, missing `created_by_user_id` |
| `SmartMeal.core/Models/Meal.cs` (MealLog) | `LogDate` as string, PostgREST DATE format |
| `SmartMeal.core/Models/MealType.cs` | Seeded lookup table, display_order |
| `SmartMeal.core/Models/Goal.cs` | UNIQUE constraint on user_id, unused goal columns |
| `SmartMeal.core/Models/WeightLog.cs` | Model + table exist, no UI yet |
| `SmartMeal.core/Models/Activity.cs` | DB-backed, UserId type history |
| `SmartMeal.core/Models/FitGoal.cs` | Marked DEPRECATED — superseded by `Goal.cs` |
| `SmartMeal.core/Services/AuthService.cs` | Two-step registration, CurrentUser lifecycle, metadata parsing |
| `SmartMeal.core/Services/MealService.cs` | All methods, upsert pattern, log_date as string |
| `SmartMeal.core/Services/FoodService.cs` | Read-only service, what each query returns |
| `SmartMeal.core/Services/GoalService.cs` | Upsert logic, why `.Get()` not `.Single()` |
| `SmartMeal.core/Services/ActService.cs` | DB-backed, GetActivitiesByUserAsync |
| `SmartMeal.Data/Context/SupabaseClientProvider.cs` | AutoRefreshToken, AutoConnectRealtime=false |
| `SmartMeal/MainWindow.xaml.cs` | Startup sequence, config validation, navigation pattern |
| `SmartMeal/Views/LoginView.xaml.cs` | Sign-in flow, success/failure handling |
| `SmartMeal/Views/RegisterView.xaml.cs` | Two-step registration, redirect on success |
| `SmartMeal/Views/DashboardView.xaml.cs` | Three data sources, balance formula, logout flow |
| `SmartMeal/Views/AddMealView.xaml.cs` | Dropdown loading, validation, DB insert |
| `SmartMeal/Views/AddActivityView.xaml.cs` | In-memory store note, validation, Activity fields |
| `SmartMeal/Views/SetGoalView.xaml.cs` | Upsert pattern, one-row-per-user constraint |
| `SmartMeal/Views/MealsView.xaml.cs` | ID-to-name resolution, `MealViewRow` projection, delete flow |

#### B. `supabase.config.json` — EmailRedirectUrl fixed

- `SupabaseEmailRedirectUrl` was set to `"https://oqkyfoaakdwxyggppijg.supabase.co"` (the project root URL).
- This is incorrect for a desktop app — there is no web callback page.
- Fixed to `""` in both `supabase.config.json` and `supabase.config.example.json`.
- `AuthService` already handles empty/null correctly: `signUpOptions.RedirectTo` is skipped when the value is null, so Supabase uses its own built-in confirmation page.

#### C. MealsView — ID-to-name resolution

- `LoadMealsAsync` now fetches `food_items` and `meal_types` in parallel (`Task.WhenAll`).
- Projects each `MealLog` into a `MealViewRow` (private sealed class) with resolved human-readable names.
- DataGrid columns now show `FoodName` and `MealTypeName` instead of raw integer IDs.

#### D. Dashboard — food name resolved in recent meal preview

- `LoadDashboardAsync` now loads `food_items` after fetching today's logs.
- Builds a `foodNameById` dictionary and resolves the latest log's `FoodId` to a display name.
- Recent Meals panel now shows e.g. `"Chicken Breast — 150g"` instead of `"Food ID 3 — 150g"`.

#### E. Sidebar navigation wired across all views

- `AddMealView`, `AddActivityView`, and `SetGoalView` all now have working sidebar `Click` handlers.
- Dashboard, Meals, and Activities links navigate correctly.
- History and Profile remain "Coming Soon" stubs (backing code exists, UI not yet built).

---

### Session 4 — 2026-04-17
**Who:** Developers
**Branch:** `main`

#### A. Registration form expanded with profile fields

- `RegisterView.xaml` — redesigned with 2-column layout for compact display:
  - Row 1: Full Name (full width)
  - Row 2: Email (full width)
  - Row 3: Password | Confirm Password (side-by-side)
  - Row 4: Age | Gender (ComboBox: Prefer not to say / Male / Female / Other)
  - Row 5: Height (cm) | Starting Weight (kg)
  - All profile fields are optional — leaving them blank is valid.
- `RegisterView.xaml.cs` — collects the new fields using `TryParse` for numerics (null if blank).
- `AuthService.RegisterAsync` — updated signature:
  ```
  RegisterAsync(name, email, password, confirmPassword, age?, heightCm?, weightKg?, gender?)
  ```
  - Validates optional fields if provided (age range 1–120, positive height/weight, valid gender string).
  - Stores all fields in Supabase Auth `user_metadata` at signup time so they survive the email-confirmation gap.
  - On auto-confirmed signup: immediately writes the full profile to `users` table.
  - On email-confirmation-required signup: fields are written to `users` at first login via `EnsureUserProfileExistsAsync`, which reads them back from `user_metadata`.

#### B. Starting weight logged to `weight_logs` at registration

- `EnsureUserProfileExistsAsync` now inserts an initial `weight_logs` row (`Notes = "Starting weight"`) when creating the `users` profile row, if a starting weight was provided.
- This ensures the weight history chart always has at least one baseline data point for users who entered their weight at registration.

#### C. `WeightHistoryView` — new view (weight graph + log weight form)

**New files:**
- `SmartMeal/Views/WeightHistoryView.xaml`
- `SmartMeal/Views/WeightHistoryView.xaml.cs`

**Features:**
- **Latest Weight card** (top right): shows the most recent weight log value and date.
- **Log Weight panel**: inline form with Weight (kg) + optional Notes fields and a "Log Weight" button. Inserts into `weight_logs` via `WeightLogService`.
- **Line graph**: drawn using WPF's built-in `Canvas`, `Polyline`, `Ellipse`, and `Line` shapes — no external charting library needed. Features:
  - Y-axis labels (weight in kg) with horizontal gridlines
  - X-axis labels (dates, up to 7 evenly spaced)
  - Blue fill polygon under the line for visual clarity
  - Filled dot at each data point with the weight value above it
  - Redraws automatically when the chart area is resized (`SizeChanged` event)
- **Time filter buttons**: 7 Days | 30 Days | All Time (active button highlighted blue)
- Sidebar navigation: Dashboard, Meals, Activities, History (current, no click), Profile (coming soon)

#### D. History button wired to WeightHistoryView across all views

Changed `History_Click` from a "Coming Soon" MessageBox to `_mainWindow.Navigate(new WeightHistoryView())` in:
- `DashboardView.xaml.cs`
- `AddMealView.xaml.cs`
- `AddActivityView.xaml.cs`
- `MealsView.xaml.cs`

#### E. Known issues updated

- Registration profile fields: ✅ now collected at registration
- Weight history: ✅ now fully implemented
- Profile editing (after registration): still not implemented

---

## 13. Architecture Notes

### How navigation works
There is no router. `MainWindow` exposes a `Navigate(UserControl view)` method that replaces the `ContentControl` (`MainContent`) content. Every view navigates by casting:
```csharp
((MainWindow)Application.Current.MainWindow).Navigate(new SomeView());
```

### How services are accessed in views
Every view grabs its services from the MainWindow in its constructor:
```csharp
_mealService = ((MainWindow)Application.Current.MainWindow).MealService;
```
This pattern is consistent across all views. It works but couples views tightly to `MainWindow`.

### How Supabase is initialised
On `MainWindow.Loaded`:
1. Reads `supabase.config.json` from the output directory
2. Validates that placeholder values have been replaced
3. Creates `SupabaseClientProvider`, calls `InitializeAsync()`
4. Instantiates all services with the single shared `Supabase.Client`

### How the `goals` upsert works
The `goals` table has a `UNIQUE` constraint on `user_id`, meaning one row per user. `GoalService.UpsertGoalAsync` reads the existing row first, then updates it if found or inserts if not. This avoids duplicate rows.

### Why `Activity.UserId` is `string`, not `Guid`
Supabase Auth user IDs are UUID strings (e.g. `"550e8400-e29b-41d4-a716-446655440000"`). The Supabase C# SDK represents them as `string`. All models that reference a user ID use `string`, not `System.Guid`, to avoid type-conversion issues.

### Why `supabase.config.json` must not be committed
The anon key is a public-facing JWT but it is still a credential scoped to this specific Supabase project. If it is exposed in a public repo, anyone can hit the project's PostgREST API. The file is in `.gitignore`. Developers joining the team must get the credentials separately and create their own local copy of the file.
