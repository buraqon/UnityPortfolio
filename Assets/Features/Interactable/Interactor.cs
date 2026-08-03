using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Interactor : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask;
    private IInteractable selectedInteractable;

    public IInteractable SelectedInteractable
    {
        get { return selectedInteractable; }
        private set
        {
            if (selectedInteractable == value) return;

            selectedInteractable?.OnDeselected?.Invoke();
            selectedInteractable = value;
            selectedInteractable?.OnSelected?.Invoke();
        }
    }

    public void Interact(Vector3 mousePos)
    {
        Interact(SelectInteractable(mousePos));
    }

    public void Interact(IInteractable interactable)
    {
        if (interactable?.IsInteractable ?? false)
            interactable.Interact(this);
    }

    public IInteractable SelectInteractable(Vector3 mousePos)
    {
        Collider[] colliders = Physics.OverlapSphere(mousePos, 1, layerMask);
        foreach (var collider in colliders)
        {
            var interactable = collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                SelectedInteractable = interactable;
                return interactable;
            }
        }
        SelectedInteractable = null;
        return null;
    }
}
