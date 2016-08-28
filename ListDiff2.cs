using System;
using System.Collections.Generic;
using System.Linq;

namespace ListDiff2
{
    public static class ListDiff2
    {
        private static ListDiffMove<T> RemoveMove<T>(int index)
        {
            return new ListDiffMove<T> { MoveType = ListDiffMove<T>.MoveTypes.Remove, Index = index };
        }

        private static ListDiffMove<T> InsertMove<T>(int index, T item)
        {
            return new ListDiffMove<T> { MoveType = ListDiffMove<T>.MoveTypes.Insert, Index = index, Item = item };
        }

        public static List<ListDiffMove<T>> Diff<T>(List<T> oldList, List<T> newList, Func<T, string> key)
        {
            return Diff<T, T>(oldList, newList, key, key);
        }

        public static List<ListDiffMove<T2>> Diff<T1, T2>(List<T1> oldList, List<T2> newList, Func<T1, string> key1,
            Func<T2, string> key2)
        {
            Dictionary<string, int> oldKeyIndex, newKeyIndex;
            List<T1> oldFree;
            List<T2> newFree;
            MapKeyIndexAndFree(oldList, key1, out oldKeyIndex, out oldFree);
            MapKeyIndexAndFree(newList, key2, out newKeyIndex, out newFree);

            var moves = new List<ListDiffMove<T2>>();

            var children = new List<T2>();
            var freeIndex = 0;

            // fist pass to check item in old list: if it's removed or not
            foreach (var item in oldList)
            {
                var itemKey = key1(item);
                if (itemKey != null)
                {
                    if (!newKeyIndex.ContainsKey(itemKey))
                    {
                        children.Add(default(T2));
                    }
                    else
                    {
                        var newItemIndex = newKeyIndex[itemKey];
                        children.Add(newList[newItemIndex]);
                    }
                }
                else
                {
                    var freeItem = newFree[freeIndex++];
                    children.Add(freeItem);
                }
            }

            // remove items no longer exist
            var simulateList = new List<T2>(children);
            for (var i = 0; i < simulateList.Count;)
            {
                if (simulateList[i] == null)
                {
                    moves.Add(RemoveMove<T2>(i));
                    simulateList.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }

            // i is cursor pointing to a item in new list
            // j is cursor pointing to a item in simulateList
            for (int i = 0, j = 0; i < newList.Count; i++)
            {
                var item = newList[i];
                var itemKey = key2(item);

                var simulateItem = simulateList.ElementAtOrDefault(j);

                if (simulateItem != null)
                {
                    var simluateItemKey = key2(simulateItem);
                    if (itemKey == simluateItemKey)
                    {
                        ++j;
                    }
                    else
                    {
                        // new item, just inesrt it
                        if (!oldKeyIndex.ContainsKey(itemKey))
                        {
                            moves.Add(InsertMove(i, item));
                        }
                        else
                        {
                            var nextItemKey = key2(simulateList[j + 1]);
                            if (nextItemKey == itemKey)
                            {
                                moves.Add(RemoveMove<T2>(i));
                                simulateList.RemoveAt(j);
                                ++j;
                            }
                            else
                            {
                                moves.Add(InsertMove(i, item));
                            }
                        }
                    }
                }
                else
                {
                    moves.Add(InsertMove(i, item));
                }
            }
            return moves;
        }

        private static void MapKeyIndexAndFree<T>(IReadOnlyList<T> list, Func<T, string> key,
            out Dictionary<string, int> keyIndex,
            out List<T> free)
        {
            keyIndex = new Dictionary<string, int>();
            free = new List<T>();

            for (var index = 0; index < list.Count; index++)
            {
                var item = list[index];
                var itemKey = key(item);
                if (itemKey != null)
                    keyIndex[itemKey] = index;
                else
                    free.Add(item);
            }
        }
    }

    public class ListDiffMove<T>
    {
        public enum MoveTypes
        {
            Remove,
            Insert
        }

        public int Index { get; set; }

        public T Item { get; set; }

        public MoveTypes MoveType { get; set; }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
            var old = new List<string> { "A", "B", "C" };
            var newList = new List<string> { "A", "C", "B" };

            var moves = ListDiff2.Diff(old, newList, str => str, str => str);
            foreach (var move in moves)
            {
                Console.WriteLine($"{move.MoveType}: {move.Index} {move.Item}");
            }
        }
    }
}