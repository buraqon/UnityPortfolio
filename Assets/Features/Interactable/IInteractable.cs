using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    void Interact(Interactor interactor);

    bool IsInteractable { get; }

    public Action OnSelected { get; set; }
    public Action OnDeselected { get; set; }
}
