using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Pair<T, U>
{
    public Pair(T _first, U _second)
    {
        first = _first;
        second = _second;
    }

    public T first;
    public U second;

    public override bool Equals(object obj)
    {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
        {
            return false;
        }
        else
        {
            Pair<T, U> p = (Pair<T, U>)obj;
            return EqualityComparer<T>.Default.Equals(first, p.first) && EqualityComparer<U>.Default.Equals(second, p.second);
        }
    }

    public override int GetHashCode()
    {
        int hash = 5;
        hash = hash * 11 + first.GetHashCode();
        hash = hash * 7 + second.GetHashCode();
        return hash;
    }
}

