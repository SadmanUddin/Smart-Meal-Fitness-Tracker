-- =========================================================
-- ADMIN + BANNED USER RLS HARDENING (SUPABASE / POSTGRES)
-- =========================================================
--
-- Run this AFTER your base schema exists.
-- Safe to re-run: policies are dropped/recreated.
--
-- What this script enforces:
-- 1) Admins (users.role = 'admin') can view all users and user-owned data tables.
-- 2) Only admins can update/delete users rows (ban/unban, role changes).
-- 3) Self-registration can only insert role='user' and is_banned=false.
-- 4) Banned users are blocked from own CRUD on meal_logs/goals/weight_logs/activities.

-- ---------------------------------------------------------
-- 0. Ensure is_banned exists
-- ---------------------------------------------------------
alter table public.users
    add column if not exists is_banned boolean not null default false;

-- ---------------------------------------------------------
-- 1. Helper functions (SECURITY DEFINER)
-- ---------------------------------------------------------
-- SECURITY DEFINER avoids RLS recursion when policies need to read public.users.
-- These functions return false if no users row exists for auth.uid().

create or replace function public.is_current_user_admin()
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    -- A banned admin loses all admin privileges at the DB level.
    -- Both role = 'admin' AND is_banned = false must be true.
    select coalesce(
        (
            select u.role = 'admin' and not u.is_banned
            from public.users u
            where u.id = auth.uid()
            limit 1
        ),
        false
    );
$$;

create or replace function public.is_current_user_banned()
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select coalesce(
        (
            select u.is_banned
            from public.users u
            where u.id = auth.uid()
            limit 1
        ),
        false
    );
$$;

revoke all on function public.is_current_user_admin() from public;
revoke all on function public.is_current_user_banned() from public;
grant execute on function public.is_current_user_admin() to authenticated;
grant execute on function public.is_current_user_banned() to authenticated;

-- ---------------------------------------------------------
-- 2. Enable RLS on app tables
-- ---------------------------------------------------------
alter table public.users enable row level security;
alter table public.meal_logs enable row level security;
alter table public.goals enable row level security;
alter table public.weight_logs enable row level security;
alter table public.activities enable row level security;

-- ---------------------------------------------------------
-- 3. USERS policies
-- ---------------------------------------------------------
-- Drop legacy policies that can conflict / over-permit.
drop policy if exists "Users can view their own profile" on public.users;
drop policy if exists "Users can insert their own profile" on public.users;
drop policy if exists "Users can update their own profile" on public.users;
drop policy if exists "Users can delete their own profile" on public.users;

-- Drop current script policies for idempotency.
drop policy if exists users_select_own on public.users;
drop policy if exists users_select_all_admin on public.users;
drop policy if exists users_insert_own on public.users;
drop policy if exists users_update_admin on public.users;
drop policy if exists users_delete_admin on public.users;

-- Users can view only their own profile row.
create policy users_select_own
on public.users
for select
using (auth.uid() = id);

-- Admins can view all users.
create policy users_select_all_admin
on public.users
for select
using (public.is_current_user_admin());

-- Users can insert only their own row at signup.
-- Prevent self-escalation to admin and self-ban on insert.
create policy users_insert_own
on public.users
for insert
with check (
    auth.uid() = id
    and role = 'user'
    and is_banned = false
);

-- Only admins can update users rows (ban/unban, role changes, corrections).
create policy users_update_admin
on public.users
for update
using (public.is_current_user_admin())
with check (public.is_current_user_admin());

-- Optional: only admins can delete users rows.
create policy users_delete_admin
on public.users
for delete
using (public.is_current_user_admin());

-- ---------------------------------------------------------
-- 4. MEAL_LOGS policies
-- ---------------------------------------------------------
drop policy if exists "Users can view their own meals" on public.meal_logs;
drop policy if exists "Users can insert their own meals" on public.meal_logs;
drop policy if exists "Users can update their own meals" on public.meal_logs;
drop policy if exists "Users can delete their own meals" on public.meal_logs;

drop policy if exists meal_logs_select_own_active on public.meal_logs;
drop policy if exists meal_logs_insert_own_active on public.meal_logs;
drop policy if exists meal_logs_update_own_active on public.meal_logs;
drop policy if exists meal_logs_delete_own_active on public.meal_logs;
drop policy if exists meal_logs_select_all_admin on public.meal_logs;

create policy meal_logs_select_own_active
on public.meal_logs
for select
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy meal_logs_insert_own_active
on public.meal_logs
for insert
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy meal_logs_update_own_active
on public.meal_logs
for update
using (user_id = auth.uid() and not public.is_current_user_banned())
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy meal_logs_delete_own_active
on public.meal_logs
for delete
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy meal_logs_select_all_admin
on public.meal_logs
for select
using (public.is_current_user_admin());

-- ---------------------------------------------------------
-- 5. GOALS policies
-- ---------------------------------------------------------
drop policy if exists "Users can view their own goals" on public.goals;
drop policy if exists "Users can insert their own goals" on public.goals;
drop policy if exists "Users can update their own goals" on public.goals;
drop policy if exists "Users can delete their own goals" on public.goals;

drop policy if exists goals_select_own_active on public.goals;
drop policy if exists goals_insert_own_active on public.goals;
drop policy if exists goals_update_own_active on public.goals;
drop policy if exists goals_delete_own_active on public.goals;
drop policy if exists goals_select_all_admin on public.goals;

create policy goals_select_own_active
on public.goals
for select
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy goals_insert_own_active
on public.goals
for insert
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy goals_update_own_active
on public.goals
for update
using (user_id = auth.uid() and not public.is_current_user_banned())
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy goals_delete_own_active
on public.goals
for delete
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy goals_select_all_admin
on public.goals
for select
using (public.is_current_user_admin());

-- ---------------------------------------------------------
-- 6. WEIGHT_LOGS policies
-- ---------------------------------------------------------
drop policy if exists "Users can view their own weight logs" on public.weight_logs;
drop policy if exists "Users can insert their own weight logs" on public.weight_logs;
drop policy if exists "Users can update their own weight logs" on public.weight_logs;
drop policy if exists "Users can delete their own weight logs" on public.weight_logs;

drop policy if exists weight_logs_select_own_active on public.weight_logs;
drop policy if exists weight_logs_insert_own_active on public.weight_logs;
drop policy if exists weight_logs_update_own_active on public.weight_logs;
drop policy if exists weight_logs_delete_own_active on public.weight_logs;
drop policy if exists weight_logs_select_all_admin on public.weight_logs;

create policy weight_logs_select_own_active
on public.weight_logs
for select
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy weight_logs_insert_own_active
on public.weight_logs
for insert
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy weight_logs_update_own_active
on public.weight_logs
for update
using (user_id = auth.uid() and not public.is_current_user_banned())
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy weight_logs_delete_own_active
on public.weight_logs
for delete
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy weight_logs_select_all_admin
on public.weight_logs
for select
using (public.is_current_user_admin());

-- ---------------------------------------------------------
-- 7. ACTIVITIES policies
-- ---------------------------------------------------------
drop policy if exists "Users can view their own activities" on public.activities;
drop policy if exists "Users can insert their own activities" on public.activities;
drop policy if exists "Users can update their own activities" on public.activities;
drop policy if exists "Users can delete their own activities" on public.activities;

drop policy if exists activities_select_own_active on public.activities;
drop policy if exists activities_insert_own_active on public.activities;
drop policy if exists activities_update_own_active on public.activities;
drop policy if exists activities_delete_own_active on public.activities;
drop policy if exists activities_select_all_admin on public.activities;

create policy activities_select_own_active
on public.activities
for select
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy activities_insert_own_active
on public.activities
for insert
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy activities_update_own_active
on public.activities
for update
using (user_id = auth.uid() and not public.is_current_user_banned())
with check (user_id = auth.uid() and not public.is_current_user_banned());

create policy activities_delete_own_active
on public.activities
for delete
using (user_id = auth.uid() and not public.is_current_user_banned());

create policy activities_select_all_admin
on public.activities
for select
using (public.is_current_user_admin());
