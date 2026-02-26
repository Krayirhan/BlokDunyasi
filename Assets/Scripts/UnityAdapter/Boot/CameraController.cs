using System;
using UnityEngine;

namespace BlockPuzzle.UnityAdapter.Boot
{
    internal sealed class CameraController
    {
        private readonly Func<bool> _canLog;

        public CameraController(Func<bool> canLog)
        {
            _canLog = canLog ?? (() => false);
        }

        public Camera SetupCamera(Vector3 cameraPosition, float cameraSize)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                if (_canLog())
                    Debug.LogWarning("[CameraController] Main Camera not found!");
                return null;
            }

            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.identity;
            camera.orthographic = true;
            camera.orthographicSize = cameraSize;

            return camera;
        }
    }
}
