using System;
using System.Collections;
using System.Collections.Generic;

namespace ModpackInstaller.Services;

public class ObservableList<T> : IEnumerable<T> {
    private List<T> _items;

    public ObservableList() {
        _items = [];
    }
    public ObservableList(List<T> items) {
        _items = items;
    }

    public event Action? Changed;

    public int IndexOf(T item) => _items.IndexOf(item);
    
    public T this[int index] {
        get => _items[index];
        set
        {
            _items[index] = value;
            Changed?.Invoke();
        }
    }
    
    public void Add(T item) {
        _items.Add(item);
        Changed?.Invoke();
    }
    
    public void AddRange(IEnumerable<T> items) {
        _items.AddRange(items);
        Changed?.Invoke();
    }

    public bool Remove(T item) {
        var result = _items.Remove(item);

        if (result)
            Changed?.Invoke();

        return result;
    }

    public void Clear() {
        _items.Clear();
        Changed?.Invoke();
    }
    
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}