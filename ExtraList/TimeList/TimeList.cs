using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace ExtraList.TimeList
{
    public class TimeList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, ICollection, IList, ITime
    {
        private T[] Values;
        private DateTime[] Times;

        #region ctor
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="size"> Размер листа. </param>
        public TimeList(int size)
        {
            Size = size;
            Values = new T[Size];
            Times = new DateTime[Size];
            _indexer = 0;
        }
        #endregion

        #region properties
        /// <summary>
        /// Размер списка
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Колличество элементов в списке
        /// </summary>
        public int Count { get { return _count; } }
        private int _count;

        /// <summary>
        /// Первый элемент на удаление
        /// </summary>
        public int Prew => _indexer - 1 == -1 ? _count - 1 : _indexer - 1;

        /// <summary>
        /// Последний элемент на уделение
        /// </summary>
        public int Next => _indexer + 1 == _count ? 0 : _indexer + 1;

        /// <summary>
        /// Указатель на место нового элемента
        /// </summary>
        public int Indexer
        {
            get { return _indexer; }
            private set { _indexer = value % Size; }
        }
        private int _indexer;
        #endregion

        public int Ordinal(int index)
        {
            if (!(index < _count)) throw new ArgumentOutOfRangeException("index");
            if (_indexer + index < _count) return _indexer + index;
            return index - (_count - _indexer);
        }

        public bool IsReadOnly => false;
        bool IList.IsFixedSize => true;

        //Indexers
        public T this[int index]
        {
            get
            {
                return Use(Ordinal(index));
            }
            set
            {
                Use(Ordinal(index));
                Values[Ordinal(index)] = value;
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                return Values[Ordinal(index)];
            }
            set
            {
                Values[Ordinal(index)] = value;
            }
        }

        DateTime ITime.this[int index]
        {
            get
            {
                return Times[Ordinal(index)];
            }
            set
            {
                Times[Ordinal(index)] = value;
            }
        }
        object IList.this[int index]
        {
            get
            {
                Use(Ordinal(index));
                return Values[Ordinal(index)];
            }
            set
            {
                if (value is T t)
                {
                    Use(Ordinal(index));
                    Values[Ordinal(index)] = t;
                }
            }
        }


        #region Add
        public int Add(object value)
        {
            if (value is T t)
            {
                Add(t);
                return Indexer;
            }
            return -1;
        }
        public void Add(T item)
        {
            Values[Indexer] = item;
            Times[Indexer] = DateTime.Now;
            if (_count < Values.Length) _count++;
            Indexer++;
        }
        public int Add(T item, DateTime time)
        {
            if (FindPlace(time, out int place))
                return -1;

            Insert(place, item, time);

            return place;
        }
        public void Add(TimeList<T> values)
        {
            DateTime[] times = values;
            for (int i = 0; i < values.Count; i++)
            {
                Add(values[i], times[i]);
            }
        }
        #endregion

        #region Contains
        public bool Contains(T item)
        {
            foreach (var t in Values)
            {
                if (t.Equals(item))
                    return true;
            }
            return false;
        }
        public bool Contains(object value)
        {
            foreach (var t in Values)
            {
                if (t.Equals(value))
                    return true;
            }
            return false;
        }
        public bool ContainsAndUse(T item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (Values[i].Equals(item))
                {
                    Use(i);
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Use
        /// <summary>
        /// Помечает предмет только что использованным.
        /// </summary>
        /// <param name="index"> Индекс используемого обьекта. </param>
        /// <returns> Возврат используемого обекта. </returns>
        public T Use(int index)
        {
            T temp = Values[index];
            int iterations = _indexer > index ? _indexer - index : Size - index - _indexer;

            if (iterations < Size / 2)
                for (int i = index; i < Indexer; i++)
                {
                    if (i == Size) i = 0;

                    int next = i + 1 == Size ? 0 : i + 1;

                    Values[i] = Values[next];
                    Times[i] = Times[next];
                }
            else
                for (int i = Indexer; i < index; i--)
                {
                    if (i == 0) i = Size - 1;

                    int prew = i - 1 == -1 ? _count - 1 : i - 1;

                    Values[i] = Values[prew];
                    Times[i] = Times[prew];
                }
            Values[index] = temp;
            Times[index] = DateTime.Now;
            return temp;
        }
        #endregion

        #region CopyTo проверить
        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = arrayIndex; i < array.Length; i++)
                Add(array[i]);
        }
        public void CopyTo(T[] array, int arrayIndex, DateTime time)
        {
            for (int i = arrayIndex; i < array.Length; i++)
                Add(array[i], time);
        }
        public void CopyTo(T[] array, int arrayIndex, int lenght)
        {
            for (int i = arrayIndex; i < arrayIndex + lenght; i++)
                Add(array[i]);
        }
        public void CopyTo(Array array, int index)
        {
            T[] values = (T[])array;
            for (int i = index; i < array.Length; i++)
                Add(values[i]);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Очистка коллекции.
        /// </summary>
        public void Clear()
        {
            Values = new T[Size];
            Times = new DateTime[Size];
        }
        #endregion

        #region IndexOf
        public int IndexOf(T item)
        {
            return Array.IndexOf(Values, item);
        }
        public int IndexOf(object value)
        {
            return Array.IndexOf(Values, (T)value);
        }
        #endregion

        #region Remove
        public bool Remove(T item)
        {
            var index = Array.IndexOf(Values, item, 0, Values.Length);

            if (index == -1) return false;

            for (var i = index; i < _count - 1; i = i == _count - 1 ? 0 : i++)
            {
                Values[i] = Values[N(i)];
                Times[i] = Times[N(i)];
            }
            _count--;

            return true;
        }
        public void Remove(object value)
        {
            if (value is T t)
            {
                var index = Array.IndexOf(Values, t, 0, Values.Length);

                if (index == -1) return;

                int _end = Old + 1 == Size ? 0 : Old + 1;
                for (var i = index; i < _end; i = i - 1 == -1 ? Size - 1 : i--)
                {
                    int prew = i - 1 == -1 ? Size - 1 : i - 1;
                    Values[i] = Values[prew];
                    Times[i] = Times[prew];
                }
                _count--;
            }

        }
        public void RemoveAt(int index)
        {
            int _end = Old + 1 == Size ? 0 : Old + 1;
            for (var i = index; i < _end; i = i - 1 == -1 ? Size - 1 : i--)
            {
                int prew = i - 1 == -1 ? Size - 1 : i - 1;
                Values[i] = Values[prew];
                Times[i] = Times[prew];
            }
            _count--;
        }
        public void RemoveAtTime(int index)
        {
            RemoveAt(_indexer - index - 1 < 0 ? Size - (index - (Size - _indexer)) - 1 : _indexer - index - 1);
        }
        #endregion

        #region Insert

        [Obsolete("Работает как метод Add()")]
        public void Insert(int index, T item) => Add(item);

        [Obsolete("Работает как метод Add()")]
        public void Insert(int index, object value) => Add(value);

        [Obsolete("Является не безопасным по отшению порядка данных в массиве")]
        public void Insert(int index, T value, DateTime time)
        {
            for (int i = Old; i < index; i = i - 1 == -1 ? Size - 1 : i--)
            {
                int prew = i - 1 == -1 ? Size - 1 : i - 1;
                Values[prew] = Values[i];
                Times[prew] = Times[i];
            }
            Values[index] = value;
            Times[index] = time;
        }

        #endregion

        #region Implicit operator
        public static implicit operator DateTime[](TimeList<T> values) { return values.GetTimeArray(); }
        public static implicit operator List<DateTime>(TimeList<T> values) { return values.GetTimeList(); }
        public static implicit operator T[](TimeList<T> values) { return values.GetValuesArray(); }
        public static implicit operator List<T>(TimeList<T> values) { return values.GetValuesList(); }

        /// <summary>
        /// Возврат массива времении обращения к обьектам.
        /// </summary>
        /// <returns> Массива времении обращения. </returns>
        public DateTime[] GetTimeArray() => Times;
        /// <summary>
        /// Возврат коллекции времении обращения к обьектам.
        /// </summary>
        /// <returns> Коллекция времении обращения. </returns>
        public List<DateTime> GetTimeList() => new List<DateTime>(Times);
        /// <summary>
        /// Возврат массива обектов содержащихся в коллекции.
        /// </summary>
        /// <returns> Массив обьектов. </returns>
        public T[] GetValuesArray() => Values;
        /// <summary>
        /// Возврат коллекции обектов содержащихся в коллекции.
        /// </summary>
        /// <returns> Коллекрия обьектов. </returns>
        public List<T> GetValuesList() => new List<T>(Values);
        #endregion

        #region Override
        public override string ToString()
        {
            StringBuilder @string = new StringBuilder();
            for (int i = Now; i < Old + 1; i = i + 1 == Size ? 0 : i++)
            {
                @string.AppendLine($"{Times[i]} - {Values[i]}");
            }
            return @string.ToString();
        }
        public override int GetHashCode()
        {
            return _count ^ _indexer + Size;
        }
        public override bool Equals(object obj)
        {
            if (obj is TimeList<T> list)
                if (list.GetTimeArray() == GetTimeArray() && list.GetValuesArray() == GetValuesArray())
                {
                    return true;
                }
            return false;

        }
        #endregion

        #region private

        /// <summary>
        /// Определяет место для вставки объекта.
        /// </summary>
        /// <param name="time"> Временной критерий вставки. </param>
        /// <param name="place"> Индекс вставки, -1 при неудаче. </param>
        /// <returns> true - индекс вставки найден, false -индекс не найден. </returns>
        private bool FindPlace(DateTime time, out int place)
        {
                place = -1;
            if (time < Times[Old])
                return false;
            else
                for (int i = Old; i < Now; i = i + 1 == Size ? 0 : i++)
            {
                    if (time < Times[i])
                {
                    place = i;
                    return true;
                }
            }
            return false;
        }

        #endregion



        #region что это блять
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)Values).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values.GetEnumerator();
        }
        #endregion

        #region это вообще хуета какая то
        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();
        #endregion
    }

    public interface ITime
    {
        public DateTime this[int index] { get; set; }
    }

}
