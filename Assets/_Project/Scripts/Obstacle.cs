using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Material _activatedMaterial;

    private float _hideDuration = 0.35f;
    private bool _activated;
    
    public void Activate()
    {
        if (_activated) return;
        _activated = true;
        _meshRenderer.material = _activatedMaterial;
        StartCoroutine(HideObstacle());
    }

    private IEnumerator HideObstacle()
    {
        float timer = _hideDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            transform.position -= Vector3.up * Time.deltaTime * 0.5f;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
