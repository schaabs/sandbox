using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace temp
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                var deck = new Deck();

                List<Card> shared = new List<Card>();

                var hole = new Card[2];

                hole[0] = ReadCard("Hole 1:");
                if (hole[0] == null) continue;
                deck.Cards.Remove(hole[0]);

                hole[1] = ReadCard("Hole 2:");
                if (hole[1] == null) continue;
                deck.Cards.Remove(hole[1]);

                var hand = new Hand(hole);

                PrintEval(hand, deck, shared);

                shared.Add(ReadCard("Flop 1:"));
                if (shared[0] == null) continue;
                deck.Cards.Remove(shared[0]);

                shared.Add(ReadCard("Flop 2:"));
                if (shared[1] == null) continue;
                deck.Cards.Remove(shared[1]);

                shared.Add(ReadCard("Flop 3:"));
                if (shared[2] == null) continue;
                deck.Cards.Remove(shared[2]);

                hand = new Hand(shared.Append(hole[0]).Append(hole[1]));

                PrintEval(hand, deck, shared);

                shared.Add(ReadCard("Turn:"));
                if (shared[3] == null) continue;
                deck.Cards.Remove(shared[3]);

                hand = new Hand(shared.Append(hole[0]).Append(hole[1]));

                PrintEval(hand, deck, shared);

                shared.Add(ReadCard("River:"));
                if (shared[4] == null) continue;
                deck.Cards.Remove(shared[4]);

                hand = new Hand(shared.Append(hole[0]).Append(hole[1]));

                PrintEval(hand, deck, shared);
                Console.ReadLine();
            }
            
        }

        static void PrintEval(Hand hand, Deck deck, List<Card> shared)
        {
            int win = 0;
            int lose = 0;
            int tie = 0;

            foreach (var h in deck.AllHands(shared))
            {
                var comp = hand.CompareTo(h);

                if (comp == -1)
                {
                    lose++;
                }
                else if (comp == 1)
                {
                    win++;
                }
                else
                {
                    tie++;
                }
            }

            Console.WriteLine();
            Console.WriteLine(hand);

            Console.WriteLine();
            Console.WriteLine("Win  :  {0:P}%", Convert.ToDouble(win) / Convert.ToDouble(win + lose + tie));
            Console.WriteLine("Lose :  {0:P}%", Convert.ToDouble(lose) / Convert.ToDouble(win + lose + tie));
            Console.WriteLine("Tie  :  {0:P}%", Convert.ToDouble(tie) / Convert.ToDouble(win + lose + tie));

            Console.WriteLine();
        }

        static Card ReadCard(string prompt)
        {
            Card c = null;

            string input = null;

            while (c == null)
            {
                Console.Write(prompt);

                input = Console.ReadLine();

                if(string.IsNullOrEmpty(input))
                {
                    return null;
                }

                if(!Card.TryParse(input, out c))
                {
                    Console.WriteLine("INVALID CARD INPUT");
                }
            }
            

            return c;
        }

        static long CountCombos(int min, List<int> cards)
        {
            long combos = 0;

            for (int i = min; i < 52 - (4 - cards.Count); i++)
            {
                cards.Add(i);

                if(cards.Count == 5)
                {
                    combos++;
                    allHands.Add(cards.ToArray());
                    //Console.WriteLine(string.Join(' ', cards));
                }
                else
                {
                    combos += CountCombos(i + 1, cards);
                }

                cards.RemoveAt(cards.Count - 1);
            }

            return combos;
        }

        static List<int[]> allHands = new List<int[]>();
    }

    public class Hand : IComparable<Hand>
    {
        public Hand(params Card[] cards) : this((IEnumerable<Card>)cards)
        {

        }

        public Hand(IEnumerable<Card> cards)
        {
            RankHand(cards);
        }

        public Rank Rank { get; private set; }

        public List<Card> Cards { get; private set; }

        public int CompareTo(Hand other)
        {
            var comp = Rank.CompareTo(other.Rank);

            if(comp != 0)
            {
                return comp;
            }

            for(int i = 0; i < Cards.Count; i++)
            {
                comp = Cards[i].Value.CompareTo(other.Cards[i].Value);

                if (comp != 0)
                {
                    return comp;
                }
            }

            return 0;
        }

        public override string ToString()
        {
            return Rank.ToString() + " " + string.Join<Card>(' ', Cards);
        }

        private void RankHand(IEnumerable<Card> cards)
        {
            var orderedCards = cards.OrderByDescending(c => c.Value).ToList();

            var stats = GetHandStats(orderedCards);

            var evaluations = new Func<List<Card>, HandStats, bool>[]
            {
                EvalForStraightFlush,
                EvalForFourOfAKind,
                EvalForFullHouse,
                EvalForFlush,
                EvalForStraight,
                EvalForThreeOfAKind,
                EvalForTwoPair,
                EvalForPair,
                EvalForHighCard
            };

            for (int i = 0; i < evaluations.Length && !evaluations[i](orderedCards, stats); i++) ;
        }

        private HandStats GetHandStats(List<Card> orderedCards)
        {

            var stats = new HandStats();

            var lastVal = -1;
            var runCount = 1;


            for (int i = 0; i < orderedCards.Count; i++)
            {
                var c = orderedCards[i];

                if (++stats.suitCount[(int)c.Suit] == 5)
                {
                    stats.isFlush = true;
                    stats.flushSuit = c.Suit;
                }

                switch (++stats.valCount[c.Value])
                {
                    case 2:
                        stats.pairCount++;
                        break;
                    case 3:
                        stats.pairCount--;
                        stats.tripCount++;
                        break;
                    case 4:
                        stats.tripCount--;
                        stats.isQuad = true;
                        break;
                }

                if (!stats.isStraight)
                {
                    if (c.Value + 1 == lastVal)
                    {
                        stats.isStraight |= ++runCount == 5;
                    }
                    else if (c.Value != lastVal)
                    {
                        stats.runIdx = i;
                        runCount = 1;
                    }

                    lastVal = c.Value;
                }
            }

            if (runCount == 4 && lastVal == 0 && stats.valCount[12] > 0)
            {
                stats.isStraight = true;
            }

            return stats;
        }

        private bool EvalForStraightFlush(List<Card> orderedCards, HandStats stats)
        {
            if (stats.isFlush && stats.isStraight)
            {
                var flushCards = orderedCards.Where(c => c.Suit == stats.flushSuit).ToList();

                var flushStats = GetHandStats(flushCards);

                if (flushStats.isStraight)
                {
                    Rank = Rank.StraighFlush;

                    Cards = GetStraightCards(flushCards, flushStats);

                    return true;
                }
            }

            return false;
        }

        private bool EvalForFourOfAKind(List<Card> orderedCards, HandStats stats)
        {
            if (stats.isQuad)
            {
                Rank = Rank.FourOfAKind;

                var quadVal = stats.valCount.LastIndexOf(4);

                Cards = new List<Card>(orderedCards.Where(c => c.Value == quadVal)) { orderedCards.First(c => c.Value != quadVal) };

                return true;
            }

            return false;
        }

        private bool EvalForFullHouse(List<Card> orderedCards, HandStats stats)
        {
            if (stats.tripCount > 1 || (stats.tripCount > 0 && stats.pairCount > 0))
            {
                Rank = Rank.FullHouse;

                var tripVal = stats.valCount.LastIndexOf(3);

                Cards = new List<Card>(orderedCards.Where(c => c.Value == tripVal));

                var pairVal = stats.tripCount > 1 ? Math.Max(stats.valCount.LastIndexOf(3, tripVal - 1), stats.valCount.LastIndexOf(2)) : stats.valCount.LastIndexOf(2);

                Cards.AddRange(orderedCards.Where(c => c.Value == pairVal).Take(2));

                return true;
            }

            return false;
        }

        private bool EvalForFlush(List<Card> orderedCards, HandStats stats)
        {
            if (stats.isFlush)
            {
                Rank = Rank.Flush;

                Cards = new List<Card>(orderedCards.Where(c => c.Suit == stats.flushSuit).Take(5));

                return true;
            }

            return false;
        }

        private bool EvalForStraight(List<Card> orderedCards, HandStats stats)
        {
            if (stats.isStraight)
            {
                Rank = Rank.Straight;

                Cards = GetStraightCards(orderedCards, stats);

                return true;
            }

            return false;
        }

        private bool EvalForThreeOfAKind(List<Card> orderedCards, HandStats stats)
        {
            if (stats.tripCount > 0)
            {
                Rank = Rank.ThreeOfAKind;

                var tripVal = stats.valCount.LastIndexOf(3);

                Cards = new List<Card>(orderedCards.Where(c => c.Value == tripVal));

                BackfillCards(orderedCards, tripVal);

                return true;
            }

            return false;
        }

        private bool EvalForTwoPair(List<Card> orderedCards, HandStats stats)
        {
            if (stats.pairCount > 1)
            {
                Rank = Rank.TwoPair;

                var pairVal1 = stats.valCount.LastIndexOf(2);

                var pairVal2 = stats.valCount.LastIndexOf(2, pairVal1 - 1);

                Cards = new List<Card>(orderedCards.Where(c => c.Value == pairVal1 || c.Value == pairVal2));

                BackfillCards(orderedCards, pairVal1, pairVal2);

                return true;
            }

            return false;
        }

        private bool EvalForPair(List<Card> orderedCards, HandStats stats)
        {
            if (stats.pairCount == 1)
            {
                Rank = Rank.Pair;

                var pairVal = stats.valCount.LastIndexOf(2);

                Cards = new List<Card>(orderedCards.Where(c => c.Value == pairVal));

                BackfillCards(orderedCards, pairVal);

                return true;
            }

            return false;
        }

        private bool EvalForHighCard(List<Card> orderedCards, HandStats stats)
        {
            Rank = Rank.HighCard;

            Cards = orderedCards.Take(5).ToList();

            return true;
        }

        private void BackfillCards(List<Card> orderedCards, params int[] skipValues)
        {
            var fillCount = 5 - Cards.Count;

            Cards.AddRange(orderedCards.Where(c => !skipValues.Contains(c.Value)).Take(fillCount));
        }

        private List<Card> GetStraightCards(List<Card> orderedCards, HandStats stats)
        {
            var cards = new List<Card>() { orderedCards[stats.runIdx] };

            for (int i = stats.runIdx + 1; i < orderedCards.Count && cards.Count < 5; i++)
            {
                if (orderedCards[i].Value != cards[cards.Count - 1].Value)
                {
                    cards.Add(orderedCards[i]);
                }
            }

            if (cards.Count < 5)
            {
                cards.Add(orderedCards[0]);
            }

            return cards;
        }

        private class HandStats
        {
            public List<int> valCount = new List<int>(new int[13]);

            public int[] suitCount = new int[4];


            public int lastVal = -1;

            public int runIdx = -1;

            public bool isStraight = false;

            public bool isFlush = false;

            public bool isQuad = false;

            public Suit flushSuit = Suit.Spade;

            public int pairCount = 0;

            public int tripCount = 0;
        }

    }
    
    public class Deck
    {
        public Deck()
        {
            foreach (var suit in new Suit[] { Suit.Spade, Suit.Club, Suit.Heart, Suit.Diamond })
            {
                for(int i = 0; i < 13; i++)
                {
                    Cards.Add(new Card() { Suit = suit, Value = i });
                }
            }
        }

        public HashSet<Card> Cards { get; private set; } = new HashSet<Card>();

        public IEnumerable<Hand> AllHands(IEnumerable<Card> shared)
        {
            var cardList = Cards.ToArray();

            for (int i = 0; i < cardList.Length - 1; i++)
            {
                for (int j = i + 1; j < cardList.Length; j++)
                {
                    yield return new Hand(shared.Append(cardList[i]).Append(cardList[j]));
                }
            }
        }

        public IEnumerable<Hand> AddCards(Hand hand, int count)
        {
            var deckCards = Cards.ToArray();

            return AddCards(deckCards, 0, new List<Card>(hand.Cards), count);
        }

        private IEnumerable<Hand> AddCards(IList<Card> deckCards, int minDeckIdx, IList<Card> handCards, int count)
        {
            for(int i = minDeckIdx; i < deckCards.Count - count; i++)
            {
                handCards.Add(deckCards[i]);

                if(count > 1)
                {
                    foreach(var h in AddCards(deckCards, i + 1, handCards, count - 1))
                    {
                        yield return h;
                    }
                }
                else
                {
                    yield return new Hand(handCards);
                }

                handCards.RemoveAt(deckCards.Count - 1);
            }
        }
    }

    public class Card
    {
        private static readonly List<char> g_suits = new List<char>() { 's', 'c', 'h', 'd' };
        private static readonly List<char> g_faces = new List<char>() { 'J', 'Q', 'K', 'A' };

        public int Value { get; set; }

        public Suit Suit { get; set; }

        public static bool TryParse(string str, out Card card)
        {
            card = null;

            if(str.Length < 2 || str.Length > 3)
            {
                return false;
            }

            str = str.ToLower();

            var valStr = str.Substring(0, str.Length - 1).ToUpper();

            char suitChar = str[str.Length - 1];

            var suitIdx = g_suits.IndexOf(suitChar);

            if(suitIdx < 0)
            {
                return false;
            }

            Suit suit = (Suit)suitIdx;
            
            int value;

            if(int.TryParse(valStr, out value))
            {
                if(value < 2 || value > 10)
                {
                    return false;
                }

                value -= 2;
            }
            else
            {
                if(valStr.Length != 1)
                {
                    return false;
                }

                var faceIdx = g_faces.IndexOf(valStr[0]);


                if (suitIdx < 0)
                {
                    return false;
                }

                value = faceIdx + 9;
            }

            card = new Card() { Value = value, Suit = suit };

            return true;
        }

        public override string ToString()
        {
            var buff = new StringBuilder();

            if(Value <= 8)
            {
                buff.Append(Value + 2);
            }
            else
            {
                buff.Append(g_faces[Value - 9]);
            }

            buff.Append(g_suits[(int)Suit]);

            return buff.ToString();
        }

        public override int GetHashCode()
        {
            return ((int)Suit) << 8 | Value;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Card;

            return other != null && other.Value == Value && other.Suit == Suit;
        }
    }       

    public enum Suit
    {
        Spade = 0,
        Club = 1,
        Heart = 2,
        Diamond = 3
    }

    public enum Rank
    {
        None = 0,
        HighCard = 1,
        Pair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraighFlush = 9
    }
}
