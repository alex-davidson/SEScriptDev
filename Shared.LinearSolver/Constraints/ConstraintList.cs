using System;
using System.Collections;
using System.Collections.Generic;

namespace Shared.LinearSolver.Constraints
{
    public class ConstraintList : IEnumerable<Constraint>
    {
        private readonly List<Constraint> constraints = new List<Constraint>();

        public int VariableCount { get; private set; }
        public int SurplusVariableCount { get; private set; }
        public int Count => constraints.Count;
        public Constraint this[int index] => constraints[index];

        public void Add(Constraint constraint)
        {
            if (constraint.SurplusCoefficient != 0) SurplusVariableCount++;
            VariableCount = Math.Max(constraint.Coefficients.Length, VariableCount);
            constraints.Add(constraint);
        }

        public IEnumerator<Constraint> GetEnumerator() => constraints.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
