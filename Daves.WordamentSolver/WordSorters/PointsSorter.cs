﻿using System;
using System.Collections.Generic;

namespace Daves.WordamentSolver.WordSorters
{
    public class PointsSorter : WordSorter, IComparer<Word>
    {
        public override string Name => "points";

        public virtual int Compare(Word x, Word y)
            => x.Points != y.Points
            ? y.Points.CompareTo(x.Points)
            : WordSorter.Alphabet.Compare(x, y);

        public override void Sort(Word[] words)
            => Array.Sort(words, comparer: this);
    }
}
