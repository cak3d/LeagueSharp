﻿/*
 Copyright 2015 - 2015 SPrediction
 ArcPrediction.cs is part of SPrediction
 
 SPrediction is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SPrediction is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SPrediction. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace Caked_AIO
{
    /// <summary>
    /// Arc Prediction class
    /// </summary>
    public static class ArcPrediction
    {
        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="input">Neccesary inputs for prediction calculations</param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        public static Prediction.Result GetPrediction(Prediction.Input input)
        {
            return GetPrediction(input.Target, input.SpellWidth, input.SpellDelay, input.SpellMissileSpeed, input.SpellRange, input.SpellCollisionable, input.Path, input.AvgReactionTime, input.LastMovChangeTime, input.AvgPathLenght, input.From.To2D(), input.RangeCheckFrom.To2D());
        }

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="from">Spell casted position</param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        public static Prediction.Result GetPrediction(Obj_AI_Hero target, float width, float delay, float missileSpeed, float range, bool collisionable)
        {
            return GetPrediction(target, width, delay, missileSpeed, range, collisionable, target.GetWaypoints(), target.AvgMovChangeTime(), target.LastMovChangeTime(), target.AvgPathLenght(), ObjectManager.Player.ServerPosition.To2D(), ObjectManager.Player.ServerPosition.To2D());
        }

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="path">Waypoints of target</param>
        /// <param name="avgt">Average reaction time (in ms)</param>
        /// <param name="movt">Passed time from last movement change (in ms)</param>
        /// <param name="avgp">Average Path Lenght</param>
        /// <param name="from">Spell casted position</param>
        /// <param name="rangeCheckFrom"></param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        public static Prediction.Result GetPrediction(Obj_AI_Base target, float width, float delay, float missileSpeed, float range, bool collisionable, List<Vector2> path, float avgt, float movt, float avgp, Vector2 from, Vector2 rangeCheckFrom)
        {
            Prediction.AssertInitializationMode();

            Prediction.Result result = Prediction.GetPrediction(target, width, delay, missileSpeed, range, collisionable, SkillshotType.SkillshotCircle, path, avgt, movt, avgp, from, rangeCheckFrom);

            if (result.HitChance >= HitChance.Low && result.HitChance < HitChance.VeryHigh)
            {
                if (result.CastPosition.Distance(from) < 875.0f)
                {
                    Vector2 direction = (result.CastPosition - from).Normalized();

                    result.CastPosition = from + direction * (875f + width / 2f);

                    var targetHitBox = ClipperWrapper.DefineCircle(Prediction.GetFastUnitPosition(target, delay, missileSpeed, from), target.BoundingRadius);

                    float multp = (result.CastPosition.Distance(from) / 875.0f);

                    var arcHitBox = new Caked_AIO.Geometry.Polygon(
                                            ClipperWrapper.DefineArc(from - new Vector2(875 / 2f, 20), result.CastPosition, (float)Math.PI * multp, 410, 200 * multp),
                                            ClipperWrapper.DefineArc(from - new Vector2(875 / 2f, 20), result.CastPosition, (float)Math.PI * multp, 410, 320 * multp));

                    if (ClipperWrapper.IsIntersects(ClipperWrapper.MakePaths(targetHitBox), ClipperWrapper.MakePaths(arcHitBox)))
                        result.HitChance = (HitChance)(result.HitChance + 1);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets Aoe Prediction result
        /// </summary>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="from">Spell casted position</param>
        /// <param name="rangeCheckFrom"></param>
        /// <returns>Prediction result as <see cref="Prediction.AoeResult"/></returns>
        public static Prediction.AoeResult GetAoePrediction(float width, float delay, float missileSpeed, float range, Vector2 from, Vector2 rangeCheckFrom)
        {
            Prediction.AoeResult result = new Prediction.AoeResult();
            var enemies = HeroManager.Enemies.Where(p => p.IsValidTarget() && Prediction.GetFastUnitPosition(p, delay, 0, from).Distance(rangeCheckFrom) < range);

            foreach (Obj_AI_Hero enemy in enemies)
            {
                Prediction.Result prediction = GetPrediction(enemy, width, delay, missileSpeed, range, false, enemy.GetWaypoints(), enemy.AvgMovChangeTime(), enemy.LastMovChangeTime(), enemy.AvgPathLenght(), from, rangeCheckFrom);
                if (prediction.HitChance > HitChance.Medium)
                {
                    float multp = (result.CastPosition.Distance(from) / 875.0f);

                    var spellHitBox = new Caked_AIO.Geometry.Polygon(
                                            ClipperWrapper.DefineArc(from - new Vector2(875 / 2f, 20), result.CastPosition, (float)Math.PI * multp, 410, 200 * multp),
                                            ClipperWrapper.DefineArc(from - new Vector2(875 / 2f, 20), result.CastPosition, (float)Math.PI * multp, 410, 320 * multp));

                    var collidedEnemies = HeroManager.Enemies.AsParallel().Where(p => ClipperWrapper.IsIntersects(ClipperWrapper.MakePaths(ClipperWrapper.DefineCircle(Prediction.GetFastUnitPosition(p, delay, missileSpeed), p.BoundingRadius)), ClipperWrapper.MakePaths(spellHitBox)));
                    int collisionCount = collidedEnemies.Count();
                    if (collisionCount > result.HitCount)
                        result = prediction.ToAoeResult(collisionCount, new Collision.Result(collidedEnemies.ToList<Obj_AI_Base>(), Collision.Flags.EnemyChampions));
                }
            }

            return result;
        }
    }
}
