﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NUnit.Framework;
using Shared.LinearSolver.Constraints;
using Shared.LinearSolver.UnitTests.Debug;

namespace Shared.LinearSolver.UnitTests
{
    [TestFixture]
    public class SimplexTests
    {
        public class Case
        {
            private readonly int lineNumber;

            public Case([CallerLineNumber] int lineNumber = 0)
            {
                this.lineNumber = lineNumber;
            }

            public ConstraintList Constraints { get; } = new ConstraintList();
            public List<float> Maximise { get; } = new List<float>();
            public List<float> Minimise { get; } = new List<float>();
            public Solution Expected { get; set; }
            public string Description { get; } = null;
            public bool IsMaximise => Maximise.Any();

            public override string ToString()
            {
                var prefix = $"Line {lineNumber}: {(IsMaximise ? "Maximise" : "Minimise")}";
                if (Description != null)
                {
                    return $"{prefix} - {Description}";
                }
                return $"{prefix} - {Constraints.VariableCount} vars: {string.Join(", ", IsMaximise ? Maximise : Minimise)}";
            }
        }

        public static Case[] Cases =
        {
            new Case
            {
                // Example from https://people.bath.ac.uk/masss/ma30087/handout6.pdf
                Constraints =
                {
                    Constrain.Linear(1, 1).LessThanOrEqualTo(6),
                    Constrain.Linear(4, -1).GreaterThanOrEqualTo(8),
                    Constrain.Linear(2, 1).EqualTo(8),
                },
                Maximise = { 3, 1 },
                Expected = new Solution
                {
                    Values = new float[] { 4, 0 },
                    Optimised = 12,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example from https://brilliant.org/wiki/linear-programming/
                Constraints =
                {
                    Constrain.Linear(2, 3).LessThanOrEqualTo(90),
                    Constrain.Linear(3, 2).LessThanOrEqualTo(120),
                },
                Maximise = { 7, 5 },
                Expected = new Solution
                {
                    Values = new float[] { 36, 6 },
                    Optimised = 282,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Another example from https://brilliant.org/wiki/linear-programming/
                Constraints =
                {
                    Constrain.Linear(-1, 5).LessThanOrEqualTo(25),
                    Constrain.Linear(6, 5).LessThanOrEqualTo(60),
                    Constrain.Linear(1, 1).GreaterThanOrEqualTo(2),
                },
                Minimise = { 1, -10 },
                Expected = new Solution
                {
                    Values = new float[] { 5, 6 },
                    Optimised = -55,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Two-phase example (revisited) from page 5 of https://www.dam.brown.edu/people/huiwang/classes/am121/Archive/big_M_121_c.pdf
                Constraints =
                {
                    Constrain.Linear(1, 1).GreaterThanOrEqualTo(1),
                    Constrain.Linear(3, 2).EqualTo(6),
                },
                Maximise = { -1, 1 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 3 },
                    Optimised = 3,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example from https://en.wikipedia.org/wiki/Simplex_algorithm#Example
                Constraints =
                {
                    Constrain.Linear(3, 2, 1).LessThanOrEqualTo(10),
                    Constrain.Linear(2, 5, 3).LessThanOrEqualTo(15),
                },
                Minimise = { -2, -3, -4 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 0, 5 },
                    Optimised = -20,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example from page 521 of https://college.cengage.com/mathematics/larson/elementary_linear/4e/shared/downloads/c09s5.pdf
                Constraints =
                {
                    Constrain.Linear(2, 1, 1).LessThanOrEqualTo(50),
                    Constrain.Linear(2, 1, 0).GreaterThanOrEqualTo(36),
                    Constrain.Linear(1, 0, 1).GreaterThanOrEqualTo(10),
                },
                Maximise = { 1, 1, 2 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 36, 14 },
                    Optimised = 64,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example 1, page 523 of https://college.cengage.com/mathematics/larson/elementary_linear/4e/shared/downloads/c09s5.pdf
                Constraints =
                {
                    Constrain.Linear(3, 2, 5).LessThanOrEqualTo(18),
                    Constrain.Linear(4, 2, 3).LessThanOrEqualTo(16),
                    Constrain.Linear(2, 1, 1).GreaterThanOrEqualTo(4),
                },
                Maximise = { 3, 2, 4 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 6.5f, 1 },
                    Optimised = 17,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example 2, page 525 of https://college.cengage.com/mathematics/larson/elementary_linear/4e/shared/downloads/c09s5.pdf
                Constraints =
                {
                    Constrain.Linear(2, 3, 4).LessThanOrEqualTo(14),
                    Constrain.Linear(3, 1, 5).GreaterThanOrEqualTo(4),
                    Constrain.Linear(1, 4, 3).GreaterThanOrEqualTo(6),
                },
                Minimise = { 4, 2, 1 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 0, 2 },
                    Optimised = 2,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // Example 4.3.1 from https://math.libretexts.org/Bookshelves/Applied_Mathematics/Applied_Finite_Mathematics_(Sekhon_and_Bloom)/04%3A_Linear_Programming_The_Simplex_Method/4.03%3A_Minimization_By_The_Simplex_Method
                Constraints =
                {
                    Constrain.Linear(1, 2).GreaterThanOrEqualTo(40),
                    Constrain.Linear(1, 1).GreaterThanOrEqualTo(30),
                },
                Minimise = { 12, 16 },
                Expected = new Solution
                {
                    Values = new float[] { 20, 10 },
                    Optimised = 400,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                
                Constraints =
                {
                    Constrain.Linear(2, 1).LessThanOrEqualTo(50),
                    Constrain.Linear(1, 3).GreaterThanOrEqualTo(15),
                    Constrain.Linear(5, 6).EqualTo(60),
                },
                Maximise = { 3, 2 },
                Expected = new Solution
                {
                    Values = new float[] { 10, 5f/3 },
                    Optimised = 100f/3,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            new Case
            {
                // https://flylib.com/books/en/3.287.1.296/1/
                Constraints =
                {
                    Constrain.Linear(1, 1).EqualTo(30),
                    Constrain.Linear(2, 8).GreaterThanOrEqualTo(80),
                    Constrain.Linear(1, 0).LessThanOrEqualTo(20),
                },
                Maximise = { 400, 200 },
                Expected = new Solution
                {
                    Values = new float[] { 20, 10 },
                    Optimised = 10000,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // No solution
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, 1).EqualTo(30),
                    Constrain.Linear(2, 2).GreaterThanOrEqualTo(80),
                },
                Maximise = { 30, 20 },
                Expected = new Solution
                {
                    Result = SimplexResult.NoSolution,
                },
            },
            // Unbounded
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, -1).LessThanOrEqualTo(30),
                    Constrain.Linear(2, 2).GreaterThanOrEqualTo(80),
                },
                Maximise = { 10, 20 },
                Expected = new Solution
                {
                    Result = SimplexResult.Unbounded,
                },
            },
            // General case
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, 1).EqualTo(30),
                    Constrain.Linear(2, 8).GreaterThanOrEqualTo(70),
                    Constrain.Linear(1, 0).LessThanOrEqualTo(15),
                },
                Maximise = { 5, 3 },
                Expected = new Solution
                {
                    Values = new float[] { 15, 15 },
                    Optimised = 120,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Simple case, maximise
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, 1).LessThanOrEqualTo(30),
                    Constrain.Linear(2, 8).LessThanOrEqualTo(80),
                    Constrain.Linear(1, 0).LessThanOrEqualTo(15),
                },
                Maximise = { 5, -3 },
                Expected = new Solution
                {
                    Values = new float[] { 15, 0 },
                    Optimised = 75,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Simple case, minimise
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, 1).LessThanOrEqualTo(30),
                    Constrain.Linear(2, 8).LessThanOrEqualTo(80),
                    Constrain.Linear(1, 0).LessThanOrEqualTo(15),
                },
                Minimise = { 5, -3 },
                Expected = new Solution
                {
                    Values = new float[] { 0, 10 },
                    Optimised = -30,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Simple case, maximise
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(-1, 1).LessThanOrEqualTo(0),   // x >= y
                    Constrain.Linear(1, 0).LessThanOrEqualTo(80),   // x <= 80
                    Constrain.Linear(0, 1).LessThanOrEqualTo(15),   // y <= 15
                },
                Maximise = { 1, 1 },   // max x+y
                Expected = new Solution
                {
                    Values = new float[] { 80, 15 },
                    Optimised = 95,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Simple case, maximise
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(-1, 1).EqualTo(0),             // x == y
                    Constrain.Linear(1, 0).LessThanOrEqualTo(80),   // x <= 80
                    Constrain.Linear(0, 1).LessThanOrEqualTo(15),   // y <= 15
                },
                Maximise = { 1, 1 },   // max x+y
                Expected = new Solution
                {
                    Values = new float[] { 15, 15 },
                    Optimised = 30,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Simple case, maximise
            new Case
            {
                Constraints =
                {
                    Constrain.Linear(1, -1).LessThanOrEqualTo(0),   // x <= y
                    Constrain.Linear(1, 0).LessThanOrEqualTo(90),   // x <= 90
                    Constrain.Linear(0, 1).LessThanOrEqualTo(120),  // y <= 120
                },
                Maximise = { 0, 1 },   // max y
                Expected = new Solution
                {
                    Values = new float[] { 0, 120 },
                    Optimised = 120,
                    Result = SimplexResult.OptimalSolution,
                },
            },
            // Complex case, maximise
            new Case
            {
                // 6 variables
                Constraints =
                {
                    Constrain.Linear(1, 1, 0, 0, 0, 0).LessThanOrEqualTo(90),
                    Constrain.Linear(0, 0, 0, 1, 0, 0).LessThanOrEqualTo(90),
                    Constrain.Linear(0, 0, 1, 0, 1, 0).LessThanOrEqualTo(60),
                    Constrain.Linear(0, 0, 0, 0, 0, 1).LessThanOrEqualTo(120),
                    Constrain.Linear(1, 0, -1, 0, 0, 0).LessThanOrEqualTo(0),
                    Constrain.Linear(0, 1, 0, 1, -1, -1).LessThanOrEqualTo(0),
                },
                Maximise = { 1, 1, 1, 1, 1, 1 },   // max all
                Expected = new Solution
                {
                    Values = new float[] { 60, 30, 60, 90, 0, 120 },
                    Optimised = 360,
                    Result = SimplexResult.OptimalSolution,
                },
            },
        };

        private IDebugWriter Debug => new DebugWriter(TestContext.WriteLine);

        [TestCaseSource(nameof(Cases))]
        public void Test(Case testCase)
        {
            var baseline = SolveCaseBaseline(testCase, out _);
            var solution = SolveCase(testCase, out _, Debug);
            AssertSolution(testCase.Expected, solution);
            AssertSolversAgree(baseline, solution);
        }

        [Test, Explicit]
        public void FuzzTest()
        {
            var random = new Random();
            const int tests = 100000;

            var counts = new int[4];

            for (var i = 0; i < tests; i++)
            {
                var seed = random.Next();
                var generator = new FuzzTestGenerator(seed)
                {
                    MinimumVariables = 20,
                    MaximumVariables = 40,
                };
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                    using (cts.Token.Register(() => System.Diagnostics.Debug.Fail($"Seed {generator.Seed} exceeded time limit")))
                    {
                        var testCase = generator.GenerateCase();
                        var solution = SolveCase(testCase, out _);
                        var baseline = SolveCaseBaseline(testCase, out _);
                        AssertSolversAgree(baseline, solution);
                        counts[(int)baseline.Result]++;
                    }
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Failed for seed {seed}\n{ex}");
                }
            }

            foreach (var resultType in Enum.GetValues(typeof(SimplexResult)))
            {
                Console.WriteLine($"{resultType}: {counts[(int)resultType]}");
            }
        }

        [TestCase(755170474)]
        [TestCase(237919398)]
        [TestCase(197840323)]
        [TestCase(2026819511)]
        [TestCase(812444773)]
        [TestCase(1254073033)]
        [TestCase(2036017711)]
        [TestCase(2017604712)]
        [Timeout(1000)]
        public void TestKnownSeeds(int seed)
        {
            var generator = new FuzzTestGenerator(seed)
            {
                MinimumVariables = 20,
                MaximumVariables = 40,
            };
            var testCase = generator.GenerateCase();
            var solution = SolveCase(testCase, out _, Debug);
            var baseline = SolveCaseBaseline(testCase, out _);
            AssertSolversAgree(baseline, solution);
        }

        [Test, Explicit]
        public void PerformanceTest()
        {
            var epsilons = new List<float>();

            var random = new Random();
            var seed = random.Next();
            var generator = new FuzzTestGenerator(seed)
            {
                MinimumVariables = 20,
                MaximumVariables = 20,
            };
            const int tests = 100000;

            var counts = new int[4];

            for (var i = 0; i < tests; i++)
            {
                var testCase = generator.GenerateCase();
                var result = SolveCase(testCase, out var worstEpsilon);
                if (worstEpsilon != null) epsilons.Add(worstEpsilon.Value);
                counts[(int)result.Result]++;
            }

            foreach (var resultType in Enum.GetValues(typeof(SimplexResult)))
            {
                Console.WriteLine($"{resultType}: {counts[(int)resultType]}");
            }
            if (epsilons.Any())
            {
                Console.WriteLine($"Epsilon: worst {epsilons.Max()}, best {epsilons.Min()}");
            }
        }

        [Test, Explicit]
        public void PerformanceTestBaseline()
        {
            var epsilons = new List<float>();

            var random = new Random();
            var seed = random.Next();
            var generator = new FuzzTestGenerator(seed)
            {
                MinimumVariables = 20,
                MaximumVariables = 20,
            };
            const int tests = 100000;

            var counts = new int[4];

            for (var i = 0; i < tests; i++)
            {
                var testCase = generator.GenerateCase();
                var result = SolveCaseBaseline(testCase, out var worstEpsilon);
                if (worstEpsilon != null) epsilons.Add(worstEpsilon.Value);
                counts[(int)result.Result]++;
            }

            foreach (var resultType in Enum.GetValues(typeof(SimplexResult)))
            {
                Console.WriteLine($"{resultType}: {counts[(int)resultType]}");
            }
            if (epsilons.Any())
            {
                Console.WriteLine($"Epsilon: worst {epsilons.Max()}, best {epsilons.Min()}");
            }
        }

        private void AssertSolution(Solution expected, Solution actual)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.Result, Is.EqualTo(expected.Result));
                Assert.That(actual.Values, Is.EqualTo(expected.Values));
                Assert.That(actual.Optimised, Is.EqualTo(expected.Optimised));
            });
        }

        private void AssertSolversAgree(Solution baseline, Solution actual)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.Result, Is.EqualTo(baseline.Result));
                Assert.That(actual.Optimised, Is.EqualTo(baseline.Optimised).Within(0.00001f));
            });
        }

        private Solution SolveCase(Case testCase, out float? worstEpsilon, IDebugWriter debug = null)
        {
            var solver = SimplexSolver.Given(testCase.Constraints, debug);
            var result = testCase.IsMaximise ? solver.Maximise(testCase.Maximise.ToArray()) : solver.Minimise(testCase.Minimise.ToArray());
            worstEpsilon = GetWorstEpsilon(testCase, result);
            return result;
        }

        private Solution SolveCaseBaseline(Case testCase, out float? worstEpsilon)
        {
            worstEpsilon = null;
            alglib.minlpcreate(testCase.Constraints.VariableCount, out var state);
            if (testCase.IsMaximise)
            {
                alglib.minlpsetcost(state, testCase.Maximise.Select(x => -(double)x).ToArray());
            }
            else
            {
                alglib.minlpsetcost(state, testCase.Minimise.Select(x => (double)x).ToArray());
            }

            alglib.minlpsetbcall(state, 0, double.PositiveInfinity);

            // Epsilon 0 = 'figure it out'.
            alglib.minlpsetalgodss(state, 0);
            foreach (var constraint in testCase.Constraints)
            {
                var coefficients = constraint.Coefficients.Select(x => (double)x).ToArray();
                switch (constraint.SurplusCoefficient)
                {
                    case -1: // >=
                        alglib.minlpaddlc2dense(state, coefficients, constraint.Target, double.PositiveInfinity);
                        break;

                    case 0: // ==
                        alglib.minlpaddlc2dense(state, coefficients, constraint.Target, constraint.Target);
                        break;

                    case 1: // <=
                        alglib.minlpaddlc2dense(state, coefficients, double.NegativeInfinity, constraint.Target);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            alglib.minlpoptimize(state);

            alglib.minlpresults(state, out var vec, out var rep);

            switch (rep.terminationtype)
            {
                case -4:
                    return new Solution
                    {
                        Result = SimplexResult.Unbounded,
                    };
                case -3:
                    return new Solution
                    {
                        Result = SimplexResult.NoSolution,
                    };
                case -2:
                    return new Solution
                    {
                        Result = SimplexResult.NoSolution,
                    };
                case 1:
                case 2:
                case 3:
                case 4:

                    var result = new Solution
                    {
                        Result = SimplexResult.OptimalSolution,
                        Optimised = testCase.IsMaximise ? -(float)rep.f : (float)rep.f,
                        Values = vec.Select(x => (float)x).ToArray(),
                    };
                    worstEpsilon = GetWorstEpsilon(testCase, result);
                    return result;
                case 5:
                    // Ran out of time.
                    return new Solution
                    {
                        Result = SimplexResult.NoSolution,
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float? GetWorstEpsilon(Case testCase, Solution solution)
        {
            if (solution.Result == SimplexResult.NoSolution) return null;
            if (solution.Result == SimplexResult.Unbounded) return null;

            var epsilonLimit = 1f;
            var worstEpsilon = 0f;
            foreach (var constraint in testCase.Constraints)
            {
                var acc = 0f;
                for (var i = 0; i < testCase.Constraints.VariableCount; i++)
                {
                    acc += (constraint.Coefficients[i] * solution.Values[i]);
                }
                var diff = acc - constraint.Target;

                switch (constraint.SurplusCoefficient)
                {
                    case -1: // >=
                        if (diff < 0)
                        {
                            worstEpsilon = Math.Max(Math.Abs(diff), worstEpsilon);
                            if (diff < -epsilonLimit)
                            {
                                Console.WriteLine($"FAIL: {acc} >= {constraint.Target}: diff is {diff}");
                            }
                        }
                        break;

                    case 0: // ==
                        if (diff < 0)
                        {
                            worstEpsilon = Math.Max(Math.Abs(diff), worstEpsilon);
                            if (diff < -epsilonLimit)
                            {
                                Console.WriteLine($"FAIL: {acc} == {constraint.Target}: diff is {diff}");
                            }
                        }

                        if (diff > 0)
                        {
                            worstEpsilon = Math.Max(Math.Abs(diff), worstEpsilon);
                            if (diff > epsilonLimit)
                            {
                                Console.WriteLine($"FAIL: {acc} == {constraint.Target}: diff is {diff}");
                            }
                        }
                        break;

                    case 1: // <=
                        if (diff > 0)
                        {
                            worstEpsilon = Math.Max(Math.Abs(diff), worstEpsilon);
                            if (diff > epsilonLimit)
                            {
                                Console.WriteLine($"FAIL: {acc} <= {constraint.Target}: diff is {diff}");
                            }
                        }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            if (worstEpsilon > epsilonLimit)
            {
                Assert.Fail("FATAL: Solution is not within a sane epsilon of valid.");
            }
            return worstEpsilon;
        }
    }
}
