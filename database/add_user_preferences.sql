-- Add food preference and allergy tracking to the users table.
-- Run this once in the Supabase SQL Editor.
--
-- food_preferences: comma-separated tags (e.g. "Vegetarian,Keto,High-Protein")
-- allergies:        comma-separated tags (e.g. "Nuts,Dairy,Gluten")
-- Both default to empty string so existing rows are not affected.

ALTER TABLE public.users ADD COLUMN IF NOT EXISTS food_preferences text NOT NULL DEFAULT '';
ALTER TABLE public.users ADD COLUMN IF NOT EXISTS allergies        text NOT NULL DEFAULT '';
