﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Automata;
using Microsoft.Automata.Internal;
using Microsoft.Automata.Internal.Utilities;
using Microsoft.Automata.Internal.Generated;
using System.Text.RegularExpressions;


namespace Automata.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        [TestMethod]
        public void TestIgnoreCaseTransformer()
        {
            CharSetSolver solver = new CharSetSolver();
            int t = System.Environment.TickCount;
            IgnoreCaseTransformer ic = new IgnoreCaseTransformer(solver);
            //simple test first:
            BDD a2c = solver.MkRangeConstraint('a', 'c');
            BDD a2cA2C = ic.Apply(a2c);
            BDD a2cA2C_expected = a2c.Or(solver.MkRangeConstraint('A', 'C'));
            Assert.AreEqual<BDD>(a2cA2C, a2cA2C_expected);
            //
            //comprehensive test:
            //
            //test that the whole array is correct:
            // Microsoft.Automata.Internal.Generated.IgnoreCaseRelation.ignorecase
            //  (generated by:)
            //
            // IgnoreCaseRelationGenerator.Generate(
            //    "Microsoft.Automata.Internal.Generated",
            //    "IgnoreCaseRelation",
            //    @"C:\GitHub\AutomataDotNet\Automata\src\Automata\Internal\Generated");
            //
            //test that all characters in it are truly equivalent wrt the igore-case option of regex
            //
            for (int i = 0; i <= 0xFFFF; i++)
            {
                char c = (char)i;
                if (ic.IsInDomain(c))
                {
                    BDD cC = ic.Apply(solver.MkCharConstraint(c));
                    foreach (char d in solver.GenerateAllCharacters(cC))
                    {
                        Assert.IsTrue(Regex.IsMatch(d.ToString(), "^(?i:" + StringUtility.Escape(c) + ")$"));
                    }
                }
            }
            //
            //second, test that all characters outside the domain are only equivalent (up-to-case) to themsevles 
            //
            // for some reson this does not succeed, ??? some characters, e.g. '\xF7', are
            // equivalent to some other characters in the below test, but not when tested individually
            // there is a bug in Regex.IsMatch with ignore-case combined with intervals
            //
            //for (int i = 2; i <= 0xFFFD; i++)
            //{
            //    char c = (char)i;
            //    if (!ic.IsInDomain(c))
            //    {
            //        if (Regex.IsMatch(c.ToString(), @"^([\0-" + StringUtility.Escape((char)(i - 1)) + StringUtility.Escape((char)(i + 1)) + @"-\uFFFF])$", RegexOptions.IgnoreCase))
            //            Console.WriteLine(StringUtility.Escape(c));
            //    }
            //}
        }
    }
}
