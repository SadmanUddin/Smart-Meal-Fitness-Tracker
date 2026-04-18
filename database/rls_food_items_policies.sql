-- RLS policies for food_items to support user-cached USDA foods.
--
-- Run this once in the Supabase SQL Editor.
--
-- Context:
--   The app now lets users search the USDA FoodData Central API and log any food.
--   When a USDA food is logged, it is inserted into food_items as a private row
--   (is_public = false, created_by_user_id = user's UUID). These policies let
--   authenticated users INSERT and SELECT their own private food rows.
--   Public seeded foods (is_public = true) remain readable by everyone.

-- Enable RLS on food_items if not already enabled.
ALTER TABLE public.food_items ENABLE ROW LEVEL SECURITY;

-- Allow all authenticated users to read public foods (seeded data).
DROP POLICY IF EXISTS "food_items_select_public" ON public.food_items;
CREATE POLICY "food_items_select_public"
    ON public.food_items
    FOR SELECT
    TO authenticated
    USING (is_public = true);

-- Allow users to read their own private foods (USDA-sourced, cached by the app).
DROP POLICY IF EXISTS "food_items_select_own_private" ON public.food_items;
CREATE POLICY "food_items_select_own_private"
    ON public.food_items
    FOR SELECT
    TO authenticated
    USING (is_public = false AND created_by_user_id = auth.uid());

-- Allow users to insert their own private foods.
-- The CHECK constraint prevents inserting public foods via the API
-- (seeded foods are inserted via service-role key or direct DB access only).
DROP POLICY IF EXISTS "food_items_insert_own" ON public.food_items;
CREATE POLICY "food_items_insert_own"
    ON public.food_items
    FOR INSERT
    TO authenticated
    WITH CHECK (is_public = false AND created_by_user_id = auth.uid());
