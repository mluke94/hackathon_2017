using UnityEngine;
using System.Collections.Generic;
using System;

namespace Leap.Unity.DetectionExamples
{

    public class GrabShoot : MonoBehaviour
    {

        [Tooltip("Each pinch detector can draw one line at a time.")]
        [SerializeField]
        private FistDetector[] _pinchDetectors;

        [SerializeField]
        private Material _material;

        [SerializeField]
        private float _drawRadius = 0.002f;

        [SerializeField]
        private float _radius;

        [SerializeField]
        private float _length;

        [SerializeField]
        private float _initforce = 2500f;

        [SerializeField]
        private GameObject _cylinder;

        [SerializeField]
        private int _drawResolution = 8;

        [SerializeField]
        private Color _drawColor = Color.white;

        private ShootState[] _shootStates;

        void OnValidate()
        {
            _initforce = Mathf.Max(0f, _initforce);
            _length = Mathf.Max(0.05f, _length);
            _radius = Mathf.Max(0.05f, _radius);
        }

        void Awake()
        {
            if (_pinchDetectors.Length == 0)
            {
                Debug.LogWarning("No fist detectors were specified!  GrabShoot can not draw any lines without FistDetectors.");
            }
        }

        void Start()
        {
            _shootStates = new ShootState[_pinchDetectors.Length];
            for (int i = 0; i < _pinchDetectors.Length; i++)
            {
                _shootStates[i] = new ShootState();
            }
        }

        void Update()
        {
            for (int i = 0; i < _pinchDetectors.Length; i++)
            {
                var detector = _pinchDetectors[i];
                var shootState = _shootStates[i];
                //var drawState = _drawStates[i];

                if (detector.DidStartHold)
                {
                    //ShootProjectile(detector);
                }

                if (detector.DidRelease)
                {
                    shootState._rate = 1000;
                }

                if (detector.IsHolding)
                {
                    var time = DateTime.Now.TimeOfDay.TotalMilliseconds;
                    var change = time - shootState._lastTime;
                    Debug.LogWarning(time);
                    if (change > shootState._rate && change >= shootState._maxRate)
                    {
                        ShootProjectile(detector);
                        shootState._lastTime = time;
                        shootState._rate -= 100;
                    }
                }
                
            }
        }

        private void ShootProjectile(FistDetector detector)
        {
            var Temporary_Bullet_Handler = Instantiate(_cylinder, detector.transform.position, detector.transform.rotation);

            Debug.LogWarning(detector.LastActiveDirection);

            //Retrieve the Rigidbody component from the instantiated Bullet and control it.
            var Temporary_RigidBody = Temporary_Bullet_Handler.GetComponent<Rigidbody>();
            Debug.LogWarning(detector.HandModel.GetLeapHand().Direction * _initforce);

            //Tell the bullet to be "pushed" forward by an amount set by Bullet_Forward_Force.
            Temporary_RigidBody.AddForce(detector.HandModel.GetLeapHand().Direction.ToVector3() * _initforce);

            //Basic Clean Up, set the Bullets to self destruct after 10 Seconds, I am being VERY generous here, normally 3 seconds is plenty.
            Destroy(Temporary_Bullet_Handler, 3.0f);
        
    }
        private class ShootState
        {
            public double _lastTime = 0;
            public double _rate = 1000;
            public double _maxRate = 100;
        }

        private class DrawState
        {
            private List<Vector3> _vertices = new List<Vector3>();
            private List<int> _tris = new List<int>();
            private List<Vector2> _uvs = new List<Vector2>();
            private List<Color> _colors = new List<Color>();

            private GrabShoot _parent;

            private int _rings = 0;

            private Vector3 _prevRing0 = Vector3.zero;
            private Vector3 _prevRing1 = Vector3.zero;

            private Vector3 _prevNormal0 = Vector3.zero;

            private Mesh _mesh;
            private SmoothedVector3 _smoothedPosition;

            public DrawState(GrabShoot parent)
            {
                _parent = parent;
            }

            public GameObject BeginNewLine()
            {
                _rings = 0;
                _vertices.Clear();
                _tris.Clear();
                _uvs.Clear();
                _colors.Clear();

                _smoothedPosition.reset = true;

                _mesh = new Mesh();
                _mesh.name = "Line Mesh";
                _mesh.MarkDynamic();

                GameObject lineObj = new GameObject("Line Object");
                lineObj.transform.position = Vector3.zero;
                lineObj.transform.rotation = Quaternion.identity;
                lineObj.transform.localScale = Vector3.one;
                lineObj.AddComponent<MeshFilter>().mesh = _mesh;
                lineObj.AddComponent<MeshRenderer>().sharedMaterial = _parent._material;

                return lineObj;
            }

            public void UpdateLine(Vector3 position)
            {

            }

            public void FinishLine()
            {

            }


            private void addRing(Vector3 ringPosition)
            {
                _rings++;

                if (_rings == 1)
                {
                    addVertexRing();
                    addVertexRing();
                    addTriSegment();
                }

                addVertexRing();
                addTriSegment();

                Vector3 ringNormal = Vector3.zero;
                if (_rings == 2)
                {
                    Vector3 direction = ringPosition - _prevRing0;
                    float angleToUp = Vector3.Angle(direction, Vector3.up);

                    if (angleToUp < 10 || angleToUp > 170)
                    {
                        ringNormal = Vector3.Cross(direction, Vector3.right);
                    }
                    else
                    {
                        ringNormal = Vector3.Cross(direction, Vector3.up);
                    }

                    ringNormal = ringNormal.normalized;

                    _prevNormal0 = ringNormal;
                }
                else if (_rings > 2)
                {
                    Vector3 prevPerp = Vector3.Cross(_prevRing0 - _prevRing1, _prevNormal0);
                    ringNormal = Vector3.Cross(prevPerp, ringPosition - _prevRing0).normalized;
                }

                if (_rings == 2)
                {
                    updateRingVerts(0,
                                    _prevRing0,
                                    ringPosition - _prevRing1,
                                    _prevNormal0,
                                    0);
                }

                if (_rings >= 2)
                {
                    updateRingVerts(_vertices.Count - _parent._drawResolution,
                                    ringPosition,
                                    ringPosition - _prevRing0,
                                    ringNormal,
                                    0);
                    updateRingVerts(_vertices.Count - _parent._drawResolution * 2,
                                    ringPosition,
                                    ringPosition - _prevRing0,
                                    ringNormal,
                                    1);
                    updateRingVerts(_vertices.Count - _parent._drawResolution * 3,
                                    _prevRing0,
                                    ringPosition - _prevRing1,
                                    _prevNormal0,
                                    1);
                }

                _prevRing1 = _prevRing0;
                _prevRing0 = ringPosition;

                _prevNormal0 = ringNormal;
            }

            private void addVertexRing()
            {
                for (int i = 0; i < _parent._drawResolution; i++)
                {
                    _vertices.Add(Vector3.zero);  //Dummy vertex, is updated later
                    _uvs.Add(new Vector2(i / (_parent._drawResolution - 1.0f), 0));
                    _colors.Add(_parent._drawColor);
                }
            }

            //Connects the most recently added vertex ring to the one before it
            private void addTriSegment()
            {
                for (int i = 0; i < _parent._drawResolution; i++)
                {
                    int i0 = _vertices.Count - 1 - i;
                    int i1 = _vertices.Count - 1 - ((i + 1) % _parent._drawResolution);

                    _tris.Add(i0);
                    _tris.Add(i1 - _parent._drawResolution);
                    _tris.Add(i0 - _parent._drawResolution);

                    _tris.Add(i0);
                    _tris.Add(i1);
                    _tris.Add(i1 - _parent._drawResolution);
                }
            }

            private void updateRingVerts(int offset, Vector3 ringPosition, Vector3 direction, Vector3 normal, float radiusScale)
            {
                direction = direction.normalized;
                normal = normal.normalized;

                for (int i = 0; i < _parent._drawResolution; i++)
                {
                    float angle = 360.0f * (i / (float)(_parent._drawResolution));
                    Quaternion rotator = Quaternion.AngleAxis(angle, direction);
                    Vector3 ringSpoke = rotator * normal * _parent._drawRadius * radiusScale;
                    _vertices[offset + i] = ringPosition + ringSpoke;
                }
            }
        }
    }
}
