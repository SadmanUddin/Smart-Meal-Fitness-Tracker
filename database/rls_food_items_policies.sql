-- Run this once in the Supabase SQL Editor.
--
-- Fixes required for USDA food search integration:
--   1. Drop NOT NULL on food_category_id — USDA foods don't map to local categories
--   2. Enable RLS and add policies so users can insert/read their own private foods

-- 1. Make food_category_id nullable (USDA-sourced foods have no local category)
ALTER TABLE public.food_items ALTER COLUMN food_category_id DROP NOT NULL;

-- 2. Enable RLS
ALTER TABLE public.food_items ENABLE ROW LEVEL SECURITY;

-- Allow all authenticated users to read public (seeded) foods
CREATE POLICY "food_items_select_public"
    ON public.food_items
    FOR SELECT TO authenticated
    USING (is_public = true);

-- Allow users to read their own private (USDA-cached) foods
CREATE POLICY "food_items_select_own_private"
    ON public.food_items
    FOR SELECT TO authenticated
    USING (is_public = false AND created_by_user_id = auth.uid());

-- Allow users to insert their own private foods
CREATE POLICY "food_items_insert_own"
    ON public.food_items
    FOR INSERT TO authenticated
    WITH CHECK (is_public = false AND created_by_user_id = auth.uid());
