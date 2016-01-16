﻿using System;
using System.Collections.Generic;
using BvSetPair = System.Tuple<Microsoft.Automata.BDD, Microsoft.Automata.BDD>;
using BvSet_Int = System.Tuple<Microsoft.Automata.BDD, int>;
using BvSetKey = System.Tuple<int, Microsoft.Automata.BDD, Microsoft.Automata.BDD>;

namespace Microsoft.Automata
{
    /// <summary>
    /// Solver for BDDs.
    /// </summary>
    public class BDDAlgebra : IBoolAlgMinterm<BDD>
    {
        //Dictionary<BvSet_Int, BDD> restrictCache = new Dictionary<BvSet_Int, BDD>();
        Dictionary<BvSetPair, BDD> orCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BvSetPair, BDD> andCache = new Dictionary<BvSetPair, BDD>();
        Dictionary<BDD, BDD> notCache = new Dictionary<BDD, BDD>();
        Dictionary<BDD, BDD> srCache = new Dictionary<BDD, BDD>();
        Dictionary<BvSet_Int, BDD> slCache = new Dictionary<BvSet_Int, BDD>();
        //Chooser _chooser_ = new Chooser();

        BDD _True;
        BDD _False;

        MintermGenerator<BDD> mintermGen;

        /// <summary>
        /// Construct a solver for bitvector sets.
        /// </summary>
        public BDDAlgebra()
        {
            mintermGen = new MintermGenerator<BDD>(this);
            _True = new BDD(this, -1, null, null);
            _False = new BDD(this, -2, null, null);
        }

        //internalize the creation of all charsets so that any two charsets with same bit and children are the same pointers
        Dictionary<BvSetKey, BDD> bvsetCache = new Dictionary<BvSetKey, BDD>();

        internal BDD MkBvSet(int nr, BDD one, BDD zero)
        {
            var key = new BvSetKey(nr, one, zero);
            BDD set;
            if (!bvsetCache.TryGetValue(key, out set))
            {
                set = new BDD(this, nr, one, zero);
                bvsetCache[key] = set;
            }
            return set;
        }

        #region IBooleanAlgebra members

        /// <summary>
        /// Make the union of a and b
        /// </summary>
        public BDD MkOr(BDD a, BDD b)
        {
            if (a == False)
                return b;
            if (b == False)
                return a;
            if (a == True || b == True)
                return True;
            if (a == b)
                return a;

            var key = new BvSetPair(a, b); 
            BDD res;
            if (orCache.TryGetValue(key, out res))
                return res;

            if (b.Ordinal > a.Ordinal)
            {
                BDD t = MkOr(a, b.One);
                BDD f = MkOr(a, b.Zero);
                res = (t == f ? t : MkBvSet(b.Ordinal, t, f));
            }
            else if (a.Ordinal > b.Ordinal)
            {
                BDD t = MkOr(a.One, b);
                BDD f = MkOr(a.Zero, b);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }
            else //a.bit == b.bit
            {
                BDD t = MkOr(a.One, b.One);
                BDD f = MkOr(a.Zero, b.Zero);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }

            orCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the intersection of a and b
        /// </summary>
        public BDD MkAnd(BDD a, BDD b)
        {
            if (a == True)
                return b;
            if (b == True)
                return a;
            if (a == False || b == False)
                return False;
            if (a == b)
                return a;

            var key = new BvSetPair(a, b);
            BDD res;
            if (andCache.TryGetValue(key, out res))
                return res;

            if (b.Ordinal > a.Ordinal)
            {
                BDD t = MkAnd(a, b.One);
                BDD f = MkAnd(a, b.Zero);
                res = (t == f ? t : MkBvSet(b.Ordinal, t, f));
            }
            else if (a.Ordinal > b.Ordinal)
            {
                BDD t = MkAnd(a.One, b);
                BDD f = MkAnd(a.Zero, b);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }
            else //a.bit == b.bit
            {
                BDD t = MkAnd(a.One, b.One);
                BDD f = MkAnd(a.Zero, b.Zero);
                res = (t == f ? t : MkBvSet(a.Ordinal, t, f));
            }

            andCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the difference a - b
        /// </summary>
        public BDD MkDiff(BDD a, BDD b)
        {
            return MkAnd(a, MkNot(b));
        }

        /// <summary>
        /// Complement a
        /// </summary>
        public BDD MkNot(BDD a)
        {
            if (a == False)
                return True;
            if (a == True)
                return False;

            BDD neg;
            if (notCache.TryGetValue(a, out neg))
                return neg;

            neg = MkBvSet(a.Ordinal, MkNot(a.One), MkNot(a.Zero));
            notCache[a] = neg;
            return neg;
        }

        /// <summary>
        /// Intersect all sets in the enumeration
        /// </summary>
        public BDD MkAnd(IEnumerable<BDD> sets)
        {
            BDD res = True;
            foreach (BDD bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        public BDD MkAnd(params BDD[] sets)
        {
            BDD res = True;
            foreach (BDD bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        /// <summary>
        /// Take the union of all sets in the enumeration
        /// </summary>
        public BDD MkOr(IEnumerable<BDD> sets)
        {
            BDD res = False;
            foreach (BDD bdd in sets)
                res = MkOr(res, bdd);
            return res;
        }

        /// <summary>
        /// Gets the full set.
        /// </summary>
        public BDD True
        {
            get { return _True; }
        }

        /// <summary>
        /// Gets the empty set.
        /// </summary>
        public BDD False
        {
            get { return _False; }
        }

        /// <summary>
        /// Returns true if the set is nonempty.
        /// </summary>
        public bool IsSatisfiable(BDD set)
        {
            return set != False;
        }

        /// <summary>
        /// Returns true if a and b represent mathematically equal sets of characters.
        /// Two BDDs are by construction equivalent iff they are identical.
        /// </summary>
        public bool AreEquivalent(BDD a, BDD b)
        {
            return a == b;
        }

        #endregion

        #region bit-shift operations

        /// <summary>
        /// Shift all elements one bit to the right. 
        /// For example if set denotes {*0000,*1110,*1111} then 
        /// ShiftRight(set) denotes {*000,*111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftRight(BDD set)
        {
            if (set.IsLeaf)
                return set;

            if (set.Ordinal == 0)
                return True;

            BDD res;
            if (srCache.TryGetValue(set, out res))
                return res;

            BDD zero = ShiftRight(set.Zero);
            BDD one = ShiftRight(set.One);

            if (zero == one)
                res = zero;
            else
                res = MkBvSet(set.Ordinal - 1, one, zero);

            srCache[set] = res;
            return res;
        }

        /// <summary>
        /// First applies ShiftRight and then sets bit k to 0.
        /// </summary>
        public BDD ShiftRight0(BDD set, int k)
        {
            var s1 = ShiftRight(set);
            var mask = MkSetWithBitFalse(k);
            var res = MkAnd(mask, s1);
            return res;
        }

        /// <summary>
        /// Shift all elements k bits to the left. 
        /// For example if k=1 and set denotes {*0000,*1111} then 
        /// ShiftLeft(set) denotes {*00000,*00001,*11110,*11111} where * denotes any prefix of 0's or 1's.
        /// </summary>
        public BDD ShiftLeft(BDD set, int k = 1)
        {
            if (set.IsLeaf || k == 0)
                return set;
            return ShiftLeft_(set, k);
        }

        BDD ShiftLeft_(BDD set, int k)
        {
            if (set.IsLeaf || k == 0)
                return set;

            var key = new BvSet_Int(set, k);

            BDD res;
            if (slCache.TryGetValue(key, out res))
                return res;

            BDD zero = ShiftLeft_(set.Zero, k);
            BDD one = ShiftLeft_(set.One, k);

            if (zero == one)
                res = zero;
            else
                res = MkBvSet(set.Ordinal + k, one, zero);

            slCache[key] = res;
            return res;
        }

        #endregion

        #region Minterm generation

        public IEnumerable<Pair<bool[], BDD>> GenerateMinterms(params BDD[] sets)
        {
            return mintermGen.GenerateMinterms(sets);
        }

        #endregion

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is true.
        /// </summary>
        public BDD MkSetWithBitTrue(int k)
        {
            return MkBvSet(k, True, False);
        }

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is false.
        /// </summary>
        public BDD MkSetWithBitFalse(int k)
        {
            return MkBvSet(k, False, True);
        }

        public BDD MkSetFromElements(IEnumerable<uint> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom(elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<int> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom((uint)elem, maxBit));
            }
            return s;
        }

        public BDD MkSetFromElements(IEnumerable<ulong> elems, int maxBit)
        {
            var s = False;
            foreach (var elem in elems)
            {
                s = MkOr(s, MkSetFrom(elem, maxBit));
            }
            return s;
        }


        /// <summary>
        /// Make a set containing all integers whose bits up to maxBit equal n.
        /// </summary>
        /// <param name="n">the given integer</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSetFrom(uint n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        } 

        /// <summary>
        /// Make a set containing all integers whose bits up to maxBit equal n.
        /// </summary>
        /// <param name="n">the given integer</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        /// <returns></returns>
        public BDD MkSetFrom(ulong n, int maxBit)
        {
            var cs = MkSetFromRange(n, n, maxBit);
            return cs;
        }

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n when considering bits between 0 and maxBit.
        /// </summary>
        /// <param name="m">lower bound</param>
        /// <param name="n">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD MkSetFromRange(uint m, uint n, int maxBit)
        {
            if (n < m)
                return False;
            uint mask = (uint)1 << maxBit;
            return CreateFromInterval1(mask, maxBit, m, n);
        }

        /// <summary>
        /// Make the set containing all values greater than or equal to m and less than or equal to n.
        /// </summary>
        /// <param name="m">lower bound</param>
        /// <param name="n">upper bound</param>
        /// <param name="maxBit">bits above maxBit are unspecified</param>
        public BDD MkSetFromRange(ulong m, ulong n, int maxBit)
        {
            if (n < m)
                return False;
            ulong mask = (ulong)1 << maxBit;
            return CreateFromInterval1(mask, maxBit, m, n);
        }

        Dictionary<Pair<int, Pair<ulong, ulong>>, BDD> intervalCache = new Dictionary<Pair<int, Pair<ulong, ulong>>, BDD>();

        BDD CreateFromInterval1(uint mask, int bit, uint m, uint n)
        {
            BDD set;
            var pair = new Pair<ulong, ulong>((ulong)m << 32, (ulong)n);
            var key = new Pair<int, Pair<ulong, ulong>>(bit, pair);

            if (intervalCache.TryGetValue(key, out set))
                return set;

            else
            {

                if (mask == 1) //base case: LSB 
                {
                    if (n == 0)  //implies that m==0 
                        set = MkBvSet(bit, False, True);
                    else if (m == 1) //implies that n==1
                        set = MkBvSet(bit, True, False);
                    else //m=0 and n=1, thus full range from 0 to ((mask << 1)-1)
                        set = True;
                }
                else if (m == 0 && n == ((mask << 1) - 1)) //full interval
                {
                    set = True;
                }
                else //mask > 1, i.e., mask = 2^b for some b > 0, and not full interval
                {
                    //e.g. m = x41 = 100 0001, n = x59 = 101 1001, mask = x40 = 100 0000, ord = 6 = log2(b)
                    uint mb = m & mask; // e.g. mb = b
                    uint nb = n & mask; // e.g. nb = b

                    if (nb == 0) // implies that 1-branch is empty
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, n);
                        set = MkBvSet(bit, False, fcase);
                    }
                    else if (mb == mask) // implies that 0-branch is empty
                    {
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, m & ~mask, n & ~mask);
                        set = MkBvSet(bit, tcase, False);
                    }
                    else //split the interval in two
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, mask - 1);
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, 0, n & ~mask);
                        set = MkBvSet(bit, tcase, fcase);
                    }
                }
                intervalCache[key] = set;
                return set;
            }
        }

        BDD CreateFromInterval1(ulong mask, int bit, ulong m, ulong n)
        {
            BDD set;
            var pair = new Pair<ulong, ulong>(m, n);
            var key = new Pair<int, Pair<ulong, ulong>>(bit, pair);

            if (intervalCache.TryGetValue(key, out set))
                return set;

            else
            {

                if (mask == 1) //base case: LSB 
                {
                    if (n == 0)  //implies that m==0 
                        set = MkBvSet(bit, False, True);
                    else if (m == 1) //implies that n==1
                        set = MkBvSet(bit, True, False);
                    else //m=0 and n=1, thus full range from 0 to ((mask << 1)-1)
                        set = True;
                }
                else if (m == 0 && n == ((mask << 1) - 1)) //full interval
                {
                    set = True;
                }
                else //mask > 1, i.e., mask = 2^b for some b > 0, and not full interval
                {
                    //e.g. m = x41 = 100 0001, n = x59 = 101 1001, mask = x40 = 100 0000, ord = 6 = log2(b)
                    ulong mb = m & mask; // e.g. mb = b
                    ulong nb = n & mask; // e.g. nb = b

                    if (nb == 0) // implies that 1-branch is empty
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, n);
                        set = MkBvSet(bit, False, fcase);
                    }
                    else if (mb == mask) // implies that 0-branch is empty
                    {
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, m & ~mask, n & ~mask);
                        set = MkBvSet(bit, tcase, False);
                    }
                    else //split the interval in two
                    {
                        var fcase = CreateFromInterval1(mask >> 1, bit - 1, m, mask - 1);
                        var tcase = CreateFromInterval1(mask >> 1, bit - 1, 0, n & ~mask);
                        set = MkBvSet(bit, tcase, fcase);
                    }
                }
                intervalCache[key] = set;
                return set;
            }
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges and return the number of such ranges.
        /// Bits above maxBit are ignored.
        /// </summary>
        public int GetRangeCount(BDD set, int maxBit)
        {
            return ToRanges64(set, maxBit).Length;
        }

        /// <summary>
        /// Convert the set into an equivalent array of uint ranges. 
        /// Bits above maxBit are ignored.
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<uint, uint>[] ToRanges(BDD set, int maxBit)
        {
            var rc = new RangeConverter();
            return rc.ToRanges(set, maxBit);
        }

        /// <summary>
        /// Convert the set into an equivalent array of ulong ranges. 
        /// Bits above maxBit are ignored.
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<ulong, ulong>[] ToRanges64(BDD set, int maxBit)
        {
            var rc = new RangeConverter64();
            return rc.ToRanges(set, maxBit);
        }

        #region Member generation and choice
        /// <summary>
        /// Choose a member of the set uniformly, each member is chosen with equal probability. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Ordinal</param>
        /// <returns></returns>
        public uint ChooseUniformly(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            uint res = (chooser.ChooseBV32() & ~(((0xFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~((uint)1 << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | ((uint)1 << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    int leftSize = (int)ComputeDomainSize(set.Zero, set.Ordinal - 1);
                    int rightSize = (int)ComputeDomainSize(set.One, set.Ordinal - 1);
                    int choice = chooser.Choose(leftSize + rightSize);
                    if (choice < leftSize)
                    {
                        res = res & ~((uint)1 << set.Ordinal); //set the bit to 0
                        set = set.Zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.Ordinal); //set the bit to 1
                        set = set.One;
                    }
                }
            }

            return res;
        }


        /// <summary>
        /// Choose a member of the set uniformly, each member is chosen with equal probability. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits above maxBit are ignored, maxBit must be at least set.Ordinal</param>
        /// <returns></returns>
        public ulong ChooseUniformly64(Chooser chooser, BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | (1UL << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch proportional to cardinalities of left and right sides
                {
                    ulong leftSize = ComputeDomainSize(set.Zero, set.Ordinal - 1);
                    ulong rightSize = ComputeDomainSize(set.One, set.Ordinal - 1);
                    //convert both sizes proportioanally to integers
                    while (leftSize >= 0xFFFFFFFF || rightSize >= 0xFFFFFFFF)
                    {
                        leftSize = leftSize >> 1;
                        rightSize = rightSize >> 1;
                    }
                    int choice = chooser.Choose((int)(leftSize + rightSize));
                    if (choice < (int)leftSize)
                    {
                        res = res & ~((uint)1 << set.Ordinal); //set the bit to 0
                        set = set.Zero;
                    }
                    else
                    {
                        res = res | ((uint)1 << set.Ordinal); //set the bit to 1
                        set = set.One;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Choose a member of the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="chooser">element chooser</param>
        /// <param name="set">given set</param>
        /// <param name="maxBit">bits over maxBit are ignored</param>
        /// <returns></returns>
        public ulong Choose(Chooser chooser, BDD set, int maxBit)
        {
            if (set.IsEmpty)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = (chooser.ChooseBV64() & ~(((0xFFFFFFFFFFFFFFFF) << maxBit) << 1));

            while (set != True)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~((ulong)1 << set.Ordinal);
                    set = set.Zero;
                }
                else if (set.Zero == False) //the bit must be set to 1
                {
                    res = res | ((ulong)1 << set.Ordinal);
                    set = set.One;
                }
                else //choose the branch according to the bit in res
                {
                    if ((res & ((ulong)1 << set.Ordinal)) == 0)
                        set = set.Zero;
                    else
                        set = set.One;
                }
            }

            return res;
        }

        /// <summary>
        /// Get the lexicographically maximum bitvector in the set. Assumes that the set is nonempty.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <param name="maxBit">bits above maxBit are ignored, b must be at least set.Bit</param>
        /// <returns>the lexicographically largest bitvector in the set</returns>
        public ulong GetMax(BDD set, int maxBit)
        {
            if (set == False)
                throw new AutomataException(AutomataExceptionKind.CharSetMustBeNonempty);
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            ulong res = ~(0xFFFFFFFFFFFFFFFF << (maxBit + 1));

            while (!set.IsFull)
            {
                if (set.One == False) //the bit must be set to 0
                {
                    res = res & ~(1UL << set.Ordinal);
                    set = set.Zero;
                }
                else
                    set = set.One;
            }
            return res;
        }

        /// <summary>
        /// Calculate the number of elements in the set. Returns 0 when set is full and maxBit is 63.
        /// </summary>
        /// <param name="set">the given set</param>
        /// <param name="maxBit">bits above maxBit are ignored</param>
        /// <returns>the cardinality of the set</returns>
        public ulong ComputeDomainSize(BDD set, int maxBit)
        {
            if (maxBit < set.Ordinal)
                throw new AutomataException(AutomataExceptionKind.InvalidArguments);

            if (set == False)
                return 0UL;
            else if (set == True)
            {
                return ((1UL << maxBit) << 1);
            }
            else
            {
                var res = CalculateCardinality1(set);
                //sizeCache.Clear();
                if (maxBit > set.Ordinal)
                {
                    res = (1UL << (maxBit - set.Ordinal)) * res;
                }
                return res;
            }
        }

        Dictionary<BDD, ulong> sizeCache = new Dictionary<BDD, ulong>();
        private ulong CalculateCardinality1(BDD set)
        {
            ulong size;
            if (sizeCache.TryGetValue(set, out size))
                return size;

            ulong sizeL;
            ulong sizeR;
            if (set.Zero.IsEmpty)
            {
                sizeL = 0;
                if (set.One.IsFull)
                {
                    sizeR = ((uint)1 << set.Ordinal);
                }
                else
                {
                    sizeR = ((uint)1 << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            else if (set.Zero.IsFull)
            {
                sizeL = (1UL << set.Ordinal);
                if (set.One.IsEmpty)
                {
                    sizeR = 0UL;
                }
                else
                {
                    sizeR = (1UL << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            else
            {
                sizeL = (1UL << (((set.Ordinal - 1) - set.Zero.Ordinal))) * CalculateCardinality1(set.Zero);
                if (set.One == False)
                {
                    sizeR = 0UL;
                }
                else if (set.One == True)
                {
                    sizeR = (1UL << set.Ordinal);
                }
                else
                {
                    sizeR = (1UL << (((set.Ordinal - 1) - set.One.Ordinal))) * CalculateCardinality1(set.One);
                }
            }
            size = sizeL + sizeR;
            sizeCache[set] = size;
            return size;
        }

        /// <summary>
        /// Get the lexicographically minimum bitvector in the set as a ulong.
        /// Assumes that the set is nonempty and that the ordinal of the BDD is at most 63.
        /// </summary>
        /// <param name="set">the given nonempty set</param>
        /// <returns>the lexicographically smallest bitvector in the set</returns>
        public ulong GetMin(BDD set)
        {
            return set.GetMin();
        }

        #endregion

        /// <summary>
        /// Make a BDD for the concrete value i with ordinal 31
        /// </summary>
        public BDD MkSet(uint i)
        {
            var set = this.MkSetFrom(i, 31);
            return set;
        }

        /// <summary>
        /// Make a BDD for the concrete value i with ordinal 63
        /// </summary>
        public BDD MkSet(ulong i)
        {
            var set = this.MkSetFrom(i, 63);
            return set;
        }

        /// <summary>
        /// Since the BvSet is always minimal simplify only returns the set itself
        /// </summary>
        public BDD Simplify(BDD set)
        {
            return set;
        }

        /// <summary>
        /// Project away the i'th bit. Assumes that bit is nonnegative.
        /// </summary>
        public BDD ProjectBit(BDD bdd, int bit)
        {
            if (bdd.Ordinal < bit)
                return bdd;
            else if (bdd.Ordinal == bit)
                return MkOr(bdd.One, bdd.Zero);
            else
                return ProjectBit_(bdd, bit, new Dictionary<BDD, BDD>());
        }

        private BDD ProjectBit_(BDD bdd, int bit, Dictionary<BDD, BDD> cache)
        {
            BDD res;
            if (!cache.TryGetValue(bdd, out res))
            {
                if (bdd.Ordinal < bit)
                    res = bdd;
                else if (bdd.Ordinal == bit)
                    res = MkOr(bdd.One, bdd.Zero);
                else
                {
                    var bdd1 = ProjectBit_(bdd.One, bit, cache);
                    var bdd0 = ProjectBit_(bdd.Zero, bit, cache);
                    res = MkBvSet(bdd.Ordinal, bdd1, bdd0);
                }
                cache[bdd] = res;
            }
            return res;
        }


        public void Dispose()
        {
            ;
        }

        public bool IsExtensional
        {
            get { return true; }
        }


        public BDD MkSymmetricDifference(BDD p1, BDD p2)
        {
            return MkOr(MkAnd(p1, MkNot(p2)), MkAnd(p2, MkNot(p1)));
        }

        public bool CheckImplication(BDD lhs, BDD rhs)
        {
            return MkAnd(lhs, MkNot(rhs)).IsEmpty;
        }

        public bool IsAtomic
        {
            get { return false; }
        }

        public BDD GetAtom(BDD psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }
    }

    /// <summary>
    /// Solver for BDDs with leaf labels that map to predicates from a Boolean algebra over T
    /// </summary>
    public class BDDAlgebra<T> : IBoolAlgMinterm<BDD<T>>
    {
        BDD<T> _True;
        BDD<T> _False;
        MintermGenerator<BDD<T>> mintermGen;
        public readonly IBooleanAlgebra<T> LeafAlgebra;
        Func<T,T> GetId;

        public BDD<T> True
        {
            get { return _True; }
        }

        public BDD<T> False
        {
            get { return _False; }
        }

        public BDDAlgebra(IBooleanAlgebra<T> leafAlgebra)
        {
            this.LeafAlgebra = leafAlgebra;
            if (leafAlgebra.IsExtensional)
                this.GetId = (psi => psi);
            else if (leafAlgebra.IsAtomic)
                this.GetId = new PredicateTrie<T>(leafAlgebra).GetId;
            else 
                this.GetId = new PredicateIdMapper<T>(leafAlgebra).GetId;
            mintermGen = new MintermGenerator<BDD<T>>(this);
            _True = MkLeaf(leafAlgebra.True);
            _False = MkLeaf(leafAlgebra.False);
        }

        Dictionary<Tuple<BDD<T>, BDD<T>>, BDD<T>> andCache = new Dictionary<Tuple<BDD<T>, BDD<T>>, BDD<T>>();
        Dictionary<Tuple<BDD<T>, BDD<T>>, BDD<T>> orCache = new Dictionary<Tuple<BDD<T>, BDD<T>>, BDD<T>>();
        Dictionary<BDD<T>, BDD<T>> notCache = new Dictionary<BDD<T>, BDD<T>>();
        Dictionary<Tuple<int, BDD<T>, BDD<T>>, BDD<T>> nodeCache = new Dictionary<Tuple<int, BDD<T>, BDD<T>>, BDD<T>>();
        Dictionary<T, BDD<T>> leafCache = new Dictionary<T, BDD<T>>();

        internal BDD<T> MkNode(int bit, BDD<T> one, BDD<T> zero)
        {
            var key = new Tuple<int, BDD<T>, BDD<T>>(bit, one, zero);
            BDD<T> bdd;
            if (!nodeCache.TryGetValue(key, out bdd))
            {
                bdd = new BDD<T>(this, bit, one, zero);
                nodeCache[key] = bdd;
            }
            return bdd;
        }

        public BDD<T> MkLeaf(T pred)
        {
            var repr = GetId(pred);
            BDD<T> leaf;
            if (!leafCache.TryGetValue(repr, out leaf))
            {
                leaf = new BDD<T>(this, 0, pred);
                leafCache[pred] = leaf;
            }
            return leaf;
        }

        #region IBoolAlg members

        /// <summary>
        /// Make the union of a and b
        /// </summary>
        public BDD<T> MkOr(BDD<T> a, BDD<T> b)
        {
            if (a == _False)
                return b;
            if (b == _False)
                return a;
            if (a == _True || b == _True)
                return _True;
            if (a == b)
                return a;

            var key = new Tuple<BDD<T>, BDD<T>>(a, b);
            BDD<T> res;
            if (orCache.TryGetValue(key, out res))
                return res;

            if (a.IsLeaf && b.IsLeaf)
                res = MkLeaf(LeafAlgebra.MkOr(a.Leaf, b.Leaf));

            else if (a.IsLeaf || b.Ordinal > a.Ordinal)
            {
                BDD<T> t = MkOr(a, b.One);
                BDD<T> f = MkOr(a, b.Zero);
                res = (t == f ? t : MkNode(b.Ordinal, t, f));
            }
            else if (b.IsLeaf || a.Ordinal > b.Ordinal)
            {
                BDD<T> t = MkOr(a.One, b);
                BDD<T> f = MkOr(a.Zero, b);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }
            else //a.Ordinal == b.Ordinal and neither is leaf
            {
                BDD<T> t = MkOr(a.One, b.One);
                BDD<T> f = MkOr(a.Zero, b.Zero);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }

            orCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the intersection of a and b
        /// </summary>
        public BDD<T> MkAnd(BDD<T> a, BDD<T> b)
        {
            if (a == _True)
                return b;
            if (b == _True)
                return a;
            if (a == _False || b == _False)
                return _False;
            if (a == b)
                return a;

            var key = new Tuple<BDD<T>,BDD<T>>(a, b);
            BDD<T> res;
            if (andCache.TryGetValue(key, out res))
                return res;

            if (a.IsLeaf && b.IsLeaf)
                res = MkLeaf(LeafAlgebra.MkAnd(a.Leaf, b.Leaf));

            else if (a.IsLeaf || b.Ordinal > a.Ordinal)
            {
                BDD<T> t = MkAnd(a, b.One);
                BDD<T> f = MkAnd(a, b.Zero);
                res = (t == f ? t : MkNode(b.Ordinal, t, f));
            }
            else if (b.IsLeaf || a.Ordinal > b.Ordinal)
            {
                BDD<T> t = MkAnd(a.One, b);
                BDD<T> f = MkAnd(a.Zero, b);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }
            else //a.Ordinal == b.Ordinal and neither is leaf
            {
                BDD<T> t = MkAnd(a.One, b.One);
                BDD<T> f = MkAnd(a.Zero, b.Zero);
                res = (t == f ? t : MkNode(a.Ordinal, t, f));
            }

            andCache[key] = res;
            return res;
        }

        /// <summary>
        /// Make the difference a - b
        /// </summary>
        public BDD<T> MkDiff(BDD<T> a, BDD<T> b)
        {
            return MkAnd(a, MkNot(b));
        }

        /// <summary>
        /// Complement a
        /// </summary>
        public BDD<T> MkNot(BDD<T> a)
        {
            if (a == _False)
                return True;
            if (a == _True)
                return False;

            BDD<T> neg;
            if (notCache.TryGetValue(a, out neg))
                return neg;

            if (a.IsLeaf)
                neg = MkLeaf(LeafAlgebra.MkNot(a.Leaf));
            else
                neg = MkNode(a.Ordinal, MkNot(a.One), MkNot(a.Zero));
            notCache[a] = neg;
            return neg;
        }

        /// <summary>
        /// Intersect all sets in the enumeration
        /// </summary>
        public BDD<T> MkAnd(IEnumerable<BDD<T>> sets)
        {
            BDD<T> res = _True;
            foreach (BDD<T> bdd in sets)
                res = MkAnd(res, bdd);
            return res;
        }

        /// <summary>
        /// Intersect all the sets.
        /// </summary>
        public BDD<T> MkAnd(params BDD<T>[] sets)
        {
            BDD<T> res = _True;
            for (int i = 0; i < sets.Length; i++ )
                res = MkAnd(res, sets[i]);
            return res;
        }

        /// <summary>
        /// Take the union of all sets in the enumeration
        /// </summary>
        public BDD<T> MkOr(IEnumerable<BDD<T>> sets)
        {
            BDD<T> res = _False;
            foreach (BDD<T> bdd in sets)
                res = MkOr(res, bdd);
            return res;
        }

        /// <summary>
        /// Returns true if bdd is nonempty.
        /// </summary>
        public bool IsSatisfiable(BDD<T> bdd)
        {
            return bdd != _False;
        }

        /// <summary>
        /// Two BDDs are by construction equivalent iff they are identical.
        /// </summary>
        public bool AreEquivalent(BDD<T> a, BDD<T> b)
        {
            return a == b;
        }

        #endregion

        #region Minterm generation

        public IEnumerable<Pair<bool[], BDD<T>>> GenerateMinterms(params BDD<T>[] sets)
        {
            return mintermGen.GenerateMinterms(sets);
        }

        #endregion

        /// <summary>
        /// Creates the bdd that contains all elements whose k'th bit is true.
        /// </summary>
        public BDD<T> MkSetWithBitTrue(int k)
        {
            return MkNode(k, _True, _False);
        }

        /// <summary>
        /// Creates the set that contains all elements whose k'th bit is false.
        /// </summary>
        public BDD<T> MkSetWithBitFalse(int k)
        {
            return MkNode(k, _False, _True);
        }

        /// <summary>
        /// Identity function
        /// </summary>
        public BDD<T> Simplify(BDD<T> set)
        {
            return set;
        }

        /// <summary>
        /// Project away the i'th bit. Assumes that bit is nonnegative.
        /// </summary>
        public BDD<T> ProjectBit(BDD<T> bdd, int bit)
        {
            if (bdd.IsLeaf | bdd.Ordinal < bit)
                return bdd;
            else if (bdd.Ordinal == bit)
                return MkOr(bdd.One, bdd.Zero);
            else
                return ProjectBit_(bdd, bit, new Dictionary<BDD<T>, BDD<T>>());
        }

        private BDD<T> ProjectBit_(BDD<T> bdd, int bit, Dictionary<BDD<T>, BDD<T>> cache)
        {
            BDD<T> res;
            if (!cache.TryGetValue(bdd, out res))
            {
                if (bdd.IsLeaf | bdd.Ordinal < bit)
                    res = bdd;
                else if (bdd.Ordinal == bit)
                    res = MkOr(bdd.One, bdd.Zero);
                else
                {
                    var bdd1 = ProjectBit_(bdd.One, bit, cache);
                    var bdd0 = ProjectBit_(bdd.Zero, bit, cache);
                    res = MkNode(bdd.Ordinal, bdd1, bdd0);
                }
                cache[bdd] = res;
            }
            return res;
        }


        public void Dispose()
        {
            ;
        }

        public bool IsExtensional
        {
            get { return true; }
        }

        /// <summary>
        /// Returns p-q.
        /// </summary>
        public BDD<T> MkDifference(BDD<T> p, BDD<T> q)
        {
            return MkAnd(p, MkNot(q));
        }

        /// <summary>
        /// Returns (p-q)|(q-p).
        /// </summary>
        public BDD<T> MkSymmetricDifference(BDD<T> p, BDD<T> q)
        {
            return MkOr(MkAnd(p, MkNot(q)), MkAnd(q, MkNot(p))); 
        }

        public bool CheckImplication(BDD<T> lhs, BDD<T> rhs)
        {
            return MkAnd(lhs, MkNot(rhs)).IsEmpty;
        }

        public bool IsAtomic
        {
            get { return false; }
        }

        public BDD<T> GetAtom(BDD<T> psi)
        {
            throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);
        }
    }

    /// <summary>
    /// For a given Boolean algebra maps all predicates to unique representatives.
    /// </summary>
    internal class PredicateIdMapper<T>
    {
        Dictionary<T, T> predMap = new Dictionary<T, T>();
        List<T> preds = new List<T>();
        IBooleanAlgebra<T> algebra;

        internal PredicateIdMapper(IBooleanAlgebra<T> algebra)
        {
            this.algebra = algebra;
        }

        /// <summary>
        /// For all p: p is equivalent to GetId(p).
        /// For all p and q: if p is equivalent to q then GetId(p)==GetId(q).
        /// </summary>
        /// <param name="p">given predicate</param>
        public T GetId(T pred)
        {
            T rep;
            if (predMap.TryGetValue(pred, out rep))
                return rep;
            else
            {
                foreach (var p in preds)
                {
                    if (algebra.AreEquivalent(pred, p))
                    {
                        predMap[pred] = p;
                        return p;
                    }
                }
                predMap[pred] = pred;
                preds.Add(pred);
                return pred;
            }
        }
    }

    /// <summary>
    /// For a given atomic Boolean algebra uses a trie of atoms to map all predicates to unique equivalent representatives.
    /// </summary>
    public class PredicateTrie<T>
    {
        Dictionary<T, T> idCache = new Dictionary<T, T>();
        IBooleanAlgebra<T> algebra;
        List<T> atoms = new List<T>();
        TrieTree tree;

        /// <summary>
        /// Creates internally a trie of atoms to distinguish predicates. Throws AutomataException if algebra.IsAtomic is false.
        /// </summary>
        /// <param name="algebra">given atomic Boolean algebra</param>
        public PredicateTrie(IBooleanAlgebra<T> algebra)
        {
            if (!algebra.IsAtomic)
                throw new AutomataException(AutomataExceptionKind.BooleanAlgebraIsNotAtomic);

            this.algebra = algebra;
            idCache[algebra.True] = algebra.True;
            idCache[algebra.False] = algebra.False;
            tree = TrieTree.MkInitialTree(algebra);
            atoms.Add(algebra.GetAtom(algebra.True)); //any element distinguishes True from False
        }

        /// <summary>
        /// For all p: p is equivalent to GetId(p).
        /// For all p and q: if p is equivalent to q then GetId(p)==GetId(q).
        /// </summary>
        /// <param name="p">given predicate</param>
        public T GetId(T p)
        {
            T id;
            if (!idCache.TryGetValue(p, out id))
            {
                id = Insert(tree, p);
                idCache[p] = id;
            }
            return id;
        }

        T Insert(TrieTree tr, T pred) 
        {
            if (tr.IsLeaf)
            {
                var leaf = tr.leaf;
                if (tr.k < atoms.Count) 
                {
                    #region extend the trie using atoms[tr.k]
                    var vk = atoms[tr.k];
                    tr.leaf = default(T);
                    if (algebra.CheckImplication(vk, leaf))
                    {
                        tr.t1 = new TrieTree(tr.k + 1, leaf, null, null);
                        if (algebra.CheckImplication(vk, pred))
                            return Insert(tr.t1, pred);
                        else
                        {
                            //k is smallest such that vk distinguishes leaf and pred
                            tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                            return pred; //pred is new
                        }
                    }
                    else
                    {
                        tr.t0 = new TrieTree(tr.k + 1, leaf, null, null);
                        if (algebra.CheckImplication(vk, pred))
                        {
                            //k is smallest such that vk distinguishes leaf and pred
                            tr.t1 = new TrieTree(tr.k + 1, pred, null, null);
                            return pred; //pred is new
                        }
                        else
                            return Insert(tr.t0, pred);
                    }
                    #endregion
                }
                else
                {
                    #region the existing atoms did not distinguish pred from leaf
                    var symdiff = algebra.MkSymmetricDifference(leaf, pred);
                    var atom = algebra.GetAtom(symdiff);
                    if (atom.Equals(algebra.False))
                        return leaf;  //pred is equivalent to leaf
                    else
                    {
                        //split the leaf based on the new atom
                        atoms.Add(atom);
                        if (algebra.CheckImplication(atom, leaf))
                        {
                            tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                            tr.t1 = new TrieTree(tr.k + 1, leaf, null, null);
                        }
                        else
                        {
                            tr.t0 = new TrieTree(tr.k + 1, leaf, null, null);
                            tr.t1 = new TrieTree(tr.k + 1, pred, null, null);     
                        }
                        tr.leaf = default(T);
                        return pred; //pred is new
                    }
                    #endregion
                }
            }
            else
            {   
                #region in a nonleaf the invariant holds: tr.k < atoms.Count
                if (algebra.CheckImplication(atoms[tr.k], pred))
                {
                    if (tr.t1 == null)
                    {
                        tr.t1 = new TrieTree(tr.k + 1, pred, null, null);
                        return pred;
                    }
                    else
                        return Insert(tr.t1, pred);
                }
                else
                {
                    if (tr.t0 == null)
                    {
                        tr.t0 = new TrieTree(tr.k + 1, pred, null, null);
                        return pred;
                    }
                    else
                        return Insert(tr.t0, pred);
                }
                #endregion
            }
        }

        private class TrieTree
        {
            /// <summary>
            /// the case when the kth atom does not imply the predicate
            /// </summary>
            internal TrieTree t0;
            /// <summary>
            /// the case when the kth atom implies the predicate
            /// </summary>
            internal TrieTree t1;
            /// <summary>
            /// distance from the root, or atom identifier
            /// </summary>
            internal readonly int k;
            /// <summary>
            /// leaf predicate
            /// </summary>
            internal T leaf;

            internal bool IsLeaf
            {
                get { return t0 == null && t1 == null; }
            }

            internal TrieTree(int k, T leaf, TrieTree t0, TrieTree t1) 
            {
                this.k = k;
                this.leaf = leaf;
                this.t0 = t0;
                this.t1 = t1;
            }

            internal static TrieTree MkInitialTree(IBooleanAlgebra<T> algebra)
            {
                var t0 = new TrieTree(1, algebra.False, null, null); 
                var t1 = new TrieTree(1, algebra.True, null, null); 
                var tree = new TrieTree(0, default(T), t0, t1);    // any element implies True and does not imply False
                return tree;
            }
        }
    }

    internal class RangeConverter
    {
        Dictionary<BDD, Pair<uint, uint>[]> rangeCache = new Dictionary<BDD, Pair<uint, uint>[]>();

        internal RangeConverter()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Pair<uint, uint>[] LiftRanges(int b, int p, Pair<uint, uint>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            uint maximal = ((uint)1 << k) - 1;

            Pair<uint, uint>[] res = new Pair<uint, uint>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (uint i = 0; i < (1 << p); i++)
            {
                uint prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Pair<uint, uint>(range.First | prefix, range.Second | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].First == 0 && ranges[ranges.Length - 1].Second == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                List<Pair<uint, uint>> res1 = new List<Pair<uint, uint>>();
                var from = res[0].First;
                var to = res[0].Second;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].First - 1)
                        to = res[i].Second;
                    else
                    {
                        res1.Add(new Pair<uint, uint>(from, to));
                        from = res[i].First;
                        to = res[i].Second;
                    }
                }
                res1.Add(new Pair<uint, uint>(from, to));
                res = res1.ToArray();
            }

            //CheckBug(res);
            return res;
        }

        Pair<uint, uint>[] ToRanges1(BDD set)
        {
            Pair<uint, uint>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.Ordinal;
                uint mask = (uint)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Pair<uint, uint>(mask, (mask << 1) - 1);
                        ranges = new Pair<uint, uint>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        ranges = new Pair<uint, uint>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Pair<uint, uint>(ranges1[i].First | mask, ranges1[i].Second | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Pair<uint, uint>(0, mask - 1);
                        ranges = new Pair<uint, uint>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.First == 0)
                        {
                            ranges = new Pair<uint, uint>[rangesR.Length];
                            ranges[0] = new Pair<uint, uint>(0, range.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                        else
                        {
                            ranges = new Pair<uint, uint>[rangesR.Length + 1];
                            ranges[0] = new Pair<uint, uint>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.Zero.Ordinal) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Pair<uint, uint>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Second == (mask - 1))
                        {
                            ranges1.Add(new Pair<uint, uint>(last.First, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Pair<uint, uint>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Second == (mask - 1) && first.First == 0) //merge together the last and first ranges
                        {
                            ranges = new Pair<uint, uint>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Pair<uint, uint>(last.First, first.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }
                        else
                        {
                            ranges = new Pair<uint, uint>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Pair<uint, uint>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }

                    }
                    #endregion
                }
                rangeCache[set] = ranges;
            }
            return ranges;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<uint, uint>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Pair<uint, uint>[] { };
            else if (set.IsFull)
                return new Pair<uint, uint>[] { new Pair<uint, uint>(0, ((((uint)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Ordinal, ToRanges1(set));
        }
    }

    internal class RangeConverter64
    {
        Dictionary<BDD, Pair<ulong, ulong>[]> rangeCache = new Dictionary<BDD, Pair<ulong, ulong>[]>();

        internal RangeConverter64()
        {
        }

        //e.g. if b = 6 and p = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        //then res = {[0000 1010, 0000 1110], [0001 1010, 0001 1110], 
        //            [0010 1010, 0010 1110], [0011 1010, 0011 1110]}, 
        Pair<ulong, ulong>[] LiftRanges(int b, int p, Pair<ulong, ulong>[] ranges)
        {
            if (p == 0)
                return ranges;

            int k = b - p;
            ulong maximal = ((ulong)1 << k) - 1;

            var res = new Pair<ulong, ulong>[(1 << p) * (ranges.Length)];
            int j = 0;
            for (ulong i = 0; i < ((ulong)(1 << p)); i++)
            {
                ulong prefix = (i << k);
                foreach (var range in ranges)
                    res[j++] = new Pair<ulong, ulong>(range.First | prefix, range.Second | prefix);
            }

            //the range wraps around : [0...][...2^k-1][2^k...][...2^(k+1)-1]
            if (ranges[0].First == 0 && ranges[ranges.Length - 1].Second == maximal)
            {
                //merge consequtive ranges, we know that res has at least two elements here
                var res1 = new List<Pair<ulong, ulong>>();
                var from = res[0].First;
                var to = res[0].Second;
                for (int i = 1; i < res.Length; i++)
                {
                    if (to == res[i].First - 1)
                        to = res[i].Second;
                    else
                    {
                        res1.Add(new Pair<ulong, ulong>(from, to));
                        from = res[i].First;
                        to = res[i].Second;
                    }
                }
                res1.Add(new Pair<ulong, ulong>(from, to));
                res = res1.ToArray();
            }
            return res;
        }

        Pair<ulong, ulong>[] ToRanges1(BDD set)
        {
            Pair<ulong, ulong>[] ranges;
            if (!rangeCache.TryGetValue(set, out ranges))
            {
                int b = set.Ordinal;
                ulong mask = (ulong)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        var range = new Pair<ulong, ulong>(mask, (mask << 1) - 1);
                        ranges = new Pair<ulong, ulong>[] { range };
                    }
                    else //1-case is neither full nor empty
                    {
                        var ranges1 = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        ranges = new Pair<ulong, ulong>[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = new Pair<ulong, ulong>(ranges1[i].First | mask, ranges1[i].Second | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        var range = new Pair<ulong, ulong>(0, mask - 1);
                        ranges = new Pair<ulong, ulong>[] { range };
                    }
                    else
                    {
                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, ToRanges1(set.One));
                        var range = rangesR[0];
                        if (range.First == 0)
                        {
                            ranges = new Pair<ulong, ulong>[rangesR.Length];
                            ranges[0] = new Pair<ulong, ulong>(0, range.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                        else
                        {
                            ranges = new Pair<ulong, ulong>[rangesR.Length + 1];
                            ranges[0] = new Pair<ulong, ulong>(0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    var rangesL = LiftRanges(b, (b - set.Zero.Ordinal) - 1, ToRanges1(set.Zero));
                    var last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<Pair<ulong, ulong>>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                            ranges1.Add(rangesL[i]);
                        if (last.Second == (mask - 1))
                        {
                            ranges1.Add(new Pair<ulong, ulong>(last.First, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add(new Pair<ulong, ulong>(mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        var rangesR0 = ToRanges1(set.One);

                        var rangesR = LiftRanges(b, (b - set.One.Ordinal) - 1, rangesR0);

                        var first = rangesR[0];

                        if (last.Second == (mask - 1) && first.First == 0) //merge together the last and first ranges
                        {
                            ranges = new Pair<ulong, ulong>[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                                ranges[i] = rangesL[i];
                            ranges[rangesL.Length - 1] = new Pair<ulong, ulong>(last.First, first.Second | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                                ranges[rangesL.Length - 1 + i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }
                        else
                        {
                            ranges = new Pair<ulong, ulong>[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                                ranges[i] = rangesL[i];
                            for (int i = 0; i < rangesR.Length; i++)
                                ranges[rangesL.Length + i] = new Pair<ulong, ulong>(rangesR[i].First | mask, rangesR[i].Second | mask);
                        }

                    }
                    #endregion
                }
                rangeCache[set] = ranges;
            }
            return ranges;
        }

        /// <summary>
        /// Convert the set into an equivalent array of ranges. 
        /// The ranges are nonoverlapping and ordered. 
        /// </summary>
        public Pair<ulong, ulong>[] ToRanges(BDD set, int maxBit)
        {
            if (set.IsEmpty)
                return new Pair<ulong, ulong>[] { };
            else if (set.IsFull)
                return new Pair<ulong, ulong>[] { new Pair<ulong, ulong>(0, ((((ulong)1 << maxBit) << 1) - 1)) }; //note: maxBit could be 31
            else
                return LiftRanges(maxBit + 1, maxBit - set.Ordinal, ToRanges1(set));
        }
    }

    /// <summary>
    /// Boolean algebra over an atomic universe.
    /// </summary>
    public class TrivialBooleanAlgebra : IBoolAlgMinterm<bool>
    {
        public TrivialBooleanAlgebra()
        {
        }

        public IEnumerable<Pair<bool[], bool>> GenerateMinterms(params bool[] constraints)
        {
            yield return new Pair<bool[], bool>(constraints, true);
        }

        public bool True
        {
            get { return true; }
        }

        public bool False
        {
            get { return false; }
        }

        public bool MkOr(IEnumerable<bool> predicates)
        {
            foreach (var v in predicates)
                if (v) 
                    return true;
            return false;
        }

        public bool MkAnd(IEnumerable<bool> predicates)
        {
            foreach (var v in predicates)
                if (!v)
                    return false;
            return true;
        }

        public bool MkAnd(params bool[] predicates)
        {
            for (int i = 0; i < predicates.Length; i++ )
                if (!predicates[i])
                    return false;
            return true;
        }

        public bool MkNot(bool predicate)
        {
            return !predicate;
        }

        public bool AreEquivalent(bool predicate1, bool predicate2)
        {
            return predicate1 == predicate2;
        }

        public bool IsExtensional
        {
            get { return true; }
        }

        public bool Simplify(bool predicate)
        {
            return predicate;
        }

        public bool IsSatisfiable(bool predicate)
        {
            return predicate;
        }

        public bool MkAnd(bool predicate1, bool predicate2)
        {
            return predicate1 && predicate2;
        }

        public bool MkOr(bool predicate1, bool predicate2)
        {
            return predicate1 || predicate2;
        }


        public bool MkSymmetricDifference(bool p1, bool p2)
        {
            return p1 != p2;
        }

        public bool CheckImplication(bool lhs, bool rhs)
        {
            return !lhs || rhs;
        }

        public bool IsAtomic
        {
            get { return true; }
        }

        public bool GetAtom(bool psi)
        {
            return psi;
        }
    }
}
