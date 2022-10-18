using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace ExtraList.TimeList
{
    /// <summary>
    /// Точка отсчёт при обращении к коллекции через индекс
    /// </summary>
    public enum ReferencePoint
    {
        /// <summary>
        /// Отсчёт будет производить от самого давнего объекта.
        /// </summary>
        Old,
        /// <summary>
        /// Отсчёт будет производить от последного последним объекта.
        /// </summary>
        Now,
        /// <summary>
        /// Отсчёт будет производить как в обычном массиве.
        /// </summary>
        Standart,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TimeList<T> : ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, ICollection, IList, ITime
    {
        public ReferencePoint ReferencePoint = ReferencePoint.Old;

        /// <summary>
        /// Массив значений.
        /// </summary>
        private T[] Values;
        /// <summary>
        /// Массив дат обращения к значению из массива значений.
        /// </summary>
        private DateTime[] Times;

        /// <summary>
        /// Стандартная емкость.
        /// </summary>
        private const int StandardCapacity = 8;

        #region ctor

        /// <summary>
        /// Конструктор
        /// </summary>
        public TimeList() : this(StandardCapacity) { }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="size"> Емкость коллекции. </param>
        /// <param name="referencePoint"> Точка отсчёт при обращении к коллекции через индекс </param>
        public TimeList(int size, ReferencePoint referencePoint)
        {
            ReferencePoint = referencePoint;
            Capacity = size;
            Values = new T[Capacity];
            Times = new DateTime[Capacity];
            _indexer = 0;
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="size"> Емкость коллекции. </param>
        public TimeList(int size)
        {
            Capacity = size;
            Values = new T[Capacity];
            Times = new DateTime[Capacity];
            _indexer = 0;
        }
        #endregion

        #region properties
        /// <summary>
        /// Емкость коллекции
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Колличество элементов в коллекции
        /// </summary>
        public int Count { get { return _count; } }
        private int _count;

        /// <summary>
        /// Индекс элемента последного обращения
        /// </summary>
        public int Now => _indexer - 1 == -1 ? Capacity - 1 : _indexer - 1;

        /// <summary>
        /// Индекс элемента симаго давнего обращения
        /// </summary>
        public int Old => _indexer - _count < 0 ? Capacity - _count + _indexer : _indexer - _count;

        /// <summary>
        /// Указатель на место записи нового элемента
        /// </summary>
        public int Indexer
        {
            get { return _indexer; }
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
                if (!CheckIndex(index)) return default;
                return GetValue(index);
            }
            set
            {
                if (!CheckIndex(index)) return;
                GetValue(index);
                Values[index] = value;
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                if (!Index(ref index)) return default;
                return Values[index];
            }
            set
            {
                if (!Index(ref index)) return;
                Values[index] = value;
                GetValue(index);
            }
        }

        DateTime ITime.this[int index]
        {
            get
            {
                if (!Index(ref index)) return default;
                return Times[index];
            }
            set
            {
                if (!Index(ref index)) return;
                Times[index] = value;
            }
        }
        object IList.this[int index]
        {
            get
            {
                if (!Index(ref index)) return default;
                GetValue(index);
                return Values[index];
            }
            set
            {
                if (!Index(ref index)) return;
                if (value is T t)
                {
                    Values[index] = t;
                    GetValue(index);
                }
            }
        }


        #region Add
        /// <summary>
        /// Добавление объекта в коллекцию.
        /// </summary>
        /// <param name="value"> Добавляемый объект. </param>
        /// <returns> Индекс в массиве, если добавить не удалось возврат -1. </returns>
        public int Add(object value)
        {
            if (value is T t)
            {
                Add(t);
                return Indexer;
            }
            return -1;
        }
        /// <summary>
        /// Добавление объекта в коллекцию.
        /// </summary>
        /// <param name="item"> Добавляемый объект. </param>
        public void Add(T item)
        {
            Values[Indexer] = item;
            Times[Indexer] = DateTime.Now;
            if (_count < Values.Length) _count++;
            _indexer++;
        }
        /// <summary>
        /// Добавление объекта в коллекцию, с заданным временем использовани.
        /// </summary>
        /// <param name="item"> Добавляемый объект. </param>
        /// <param name="time"> Время использования. </param>
        /// <returns> Индекс в массиве, если добавить не удалось возврат -1. </returns>
        public int Add(T item, DateTime time)
        {
            if (FindPlace(time, out int place))
                return -1;

            Insert(place, item, time);
            return place;
        }
        /// <summary>
        /// Добавление коллекции с выборкой по времени.
        /// </summary>
        /// <param name="values"> Добавляемай коллекция. </param>
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
        /// <summary>
        /// Проверка наличия обекта в коллекции.
        /// </summary>
        /// <param name="item"> Проверяемый обект. </param>
        /// <returns> При наличии - true, отсутствии - false. </returns>
        public bool Contains(T item)
        {
            foreach (var t in Values)
            {
                if (t.Equals(item))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Проверка наличия обекта в коллекции.
        /// </summary>
        /// <param name="value"> Проверяемый обект. </param>
        /// <returns> При наличии - true, отсутствии - false. </returns>
        public bool Contains(object value)
        {
            foreach (var t in Values)
            {
                if (t.Equals(value))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Проверка наличия обекта в коллекции и помечает его только что использованным.
        /// </summary>
        /// <param name="item"> Проверяемый обект. </param>
        /// <returns> При наличии - true, отсутствии - false. </returns>
        public bool ContainsAndUse(T item)
        {
            for (int i = 0; i < _count; i++)
            {
                if (Values[i].Equals(item))
                    return true;
            }
            return false;
        }
        #endregion

        #region GetValue
        /// <summary>
        /// Получение объекта из коллекции.
        /// </summary>
        /// <param name="index"> Индекс получаемого обьекта. </param>
        /// <returns> Возврат используемого обекта. </returns>
        public T GetValue(int index)
        {
            if (!Index(ref index)) return default;

            T value = Values[index];

            if (index == Now)
            {
                Times[index] = DateTime.Now;
                return value;
            }
            else if (index < Now)
            {
                T[] values = new T[Now - index - 1];
                DateTime[] dates = new DateTime[Now - index - 1];

                // копирование
                Array.Copy(Values, index + 1, values, 0, Now - index - 1);
                Array.Copy(Times, index + 1, dates, 0, Now - index - 1);

                // вставка
                Array.Copy(values, 0, Values, index, Now - index - 1);
                Array.Copy(dates, 0, Times, index, Now - index - 1);
            }
            else
            {
                T[] values = new T[(Capacity - index) + Now];
                DateTime[] dates = new DateTime[(Capacity - index - 1) + Now];

                //копирование до перехода чера 0
                Array.Copy(Values, index + 1, values, 0, Capacity - index - 1);
                Array.Copy(Times, index + 1, dates, 0, Capacity - index - 1);

                //копирование после перехода чера 0
                Array.Copy(Values, 0, values, Capacity - index, Now);
                Array.Copy(Times, 0, dates, Capacity - index, Now);

                //вставка до перехода чера 0
                Array.Copy(values, 0, Values, index, Capacity - index);
                Array.Copy(dates, 0, Times, index, Capacity - index);

                //вставка после перехода чера 0
                Array.Copy(values, Capacity - index + 1, values, 0, Now - (Capacity - index));
                Array.Copy(dates, Capacity - index + 1, dates, 0, Now - (Capacity - index));
            }
            Values[index] = value;
            Times[index] = DateTime.Now;
            return value;
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
            Values = new T[Capacity];
            Times = new DateTime[Capacity];
        }
        #endregion

        #region IndexOf

        /// <summary>
        /// Получение индекса объекта в коллекции
        /// </summary>
        /// <param name="item"> Искомый объект. </param>
        /// <returns> Индекс искомого объекта. </returns>
        public int IndexOf(T item)
        {
            return Array.IndexOf(Values, item);
        }
        /// <summary>
        /// Получение индекса объекта в коллекции
        /// </summary>
        /// <param name="value"> Искомый объект. </param>
        /// <returns> Индекс искомого объекта. </returns>
        public int IndexOf(object value)
        {
            return Array.IndexOf(Values, (T)value);
        }
        #endregion

        #region Remove

        /// <summary>
        /// Удаление объекта.
        /// </summary>
        /// <param name="item"> Удаляемый объект. </param>
        /// <returns> true - объект удален, false - не удален. </returns>
        public bool Remove(T item)
        {
            var index = Array.IndexOf(Values, item, 0, Values.Length);

            if (index == -1) return false;

            int _end = Old + 1 == Capacity ? 0 : Old + 1;
            for (var i = index; i < _end; i = i - 1 == -1 ? Capacity - 1 : i--)
            {
                int prew = i - 1 == -1 ? Capacity - 1 : i - 1;
                Values[i] = Values[prew];
                Times[i] = Times[prew];
            }
            _count--;

            return true;
        }
        /// <summary>
        /// Удаление объекта.
        /// </summary>
        /// <param name="value"> Удаляемый объект. </param>
        public void Remove(object value)
        {
            if (value is T t)
            {
                var index = Array.IndexOf(Values, t, 0, Values.Length);

                if (index == -1) return;

                int _end = Old + 1 == Capacity ? 0 : Old + 1;
                for (var i = index; i < _end; i = i - 1 == -1 ? Capacity - 1 : i--)
                {
                    int prew = i - 1 == -1 ? Capacity - 1 : i - 1;
                    Values[i] = Values[prew];
                    Times[i] = Times[prew];
                }
                _count--;
            }

        }

        /// <summary>
        /// Удаление объекта.
        /// </summary>
        /// <param name="index"> Индекс удаляемого объекта. </param>
        public void RemoveAt(int index)
        {
            if (!Index(ref index)) return;

            int _end = Old + 1 == Capacity ? 0 : Old + 1;
            for (var i = index; i < _end; i = i - 1 == -1 ? Capacity - 1 : i--)
            {
                int prew = i - 1 == -1 ? Capacity - 1 : i - 1;
                Values[i] = Values[prew];
                Times[i] = Times[prew];
            }
            _count--;
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
            if (!Index(ref index)) return;

            for (int i = Old; i < index; i = i - 1 == -1 ? Capacity - 1 : i--)
            {
                int prew = i - 1 == -1 ? Capacity - 1 : i - 1;
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

        /// <summary>
        /// Преобразование коллекции в строку
        /// </summary>
        /// <returns> Строковое представление коллекции. </returns>
        public override string ToString()
        {
            StringBuilder @string = new StringBuilder();
            for (int i = Now; i < Old + 1; i = i + 1 == Capacity ? 0 : i++)
            {
                @string.AppendLine($"{Times[i]} - {(Values[i])}");
            }
            return @string.ToString();
        }

        /// <summary>
        /// Хеш код коллекции
        /// </summary>
        /// <returns> Целочисленное представление хеш кода коллекции. </returns>
        public override int GetHashCode()
        {
            return _count ^ _indexer + Capacity;
        }

        /// <summary>
        /// Сравнивание с объектом.
        /// </summary>
        /// <param name="obj"> Объект сравнения. </param>
        /// <returns> true - коллекции идентичны, false - разные </returns>
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
        /// Опредение места для вставки объекта.
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
                for (int i = Old; i < Now; i = i + 1 == Capacity ? 0 : i++)
                {
                    if (time < Times[i])
                    {
                        place = i;
                        return true;
                    }
                }
            return false;
        }

        private bool CheckIndex(int index)
        {
            return index < 0 || index >= Capacity ? throw new ArgumentOutOfRangeException(nameof(index)) : true;
        }

        private bool Index(ref int index)
        {
            if (!CheckIndex(index)) return false;

            switch (ReferencePoint)
            {
                case ReferencePoint.Standart:
                    {
                        return true;
                    }
                case ReferencePoint.Old:
                    {
                        index = Old + index >= Capacity ? index - (Capacity - Old) : Old + index;
                        return true;
                    }
                case ReferencePoint.Now:
                    {
                        index = Now - index < 0 ? Capacity - (index - Now) : Now - index;
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        #endregion



        #region что это блять
        public IEnumerator<T> GetEnumerator()
        {
            return GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
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
