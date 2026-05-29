using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Test.Scripts.PSO
{
    public class NpcBase : MonoBehaviour
    {
        public GameObject particlePrefab;

        private int _iteration = 0;
        public int maxIter = 10;

        [Header("Population Parameters")] public int popSize = 20;
        public int bestParticle;

        private float _gBestCost;
        private float[] _pBestCosts;

        private Vector3[] _velocities;
        private Vector3[] _positions;
        private Vector3[] _pBestPositions;
        private Vector3 _gBestPosition;

        private GameObject[] _particles;

        public Transform target;

        public float waitTime = 1;
        public float maxVelocity = 10;
        public float startInertia = 0.9f;
        public float endInertia = 0.4f;
        public float c1 = 2;
        public float c2 = 2;

        public string direction = "";
        private Vector2[] _waypoints;
        private Vector3 _robotPos;

        private float _xMax;
        private float _yMax;

        private bool _firstChange;
        
        public Vector2 maxRandPosition;
        public Vector2 minRandPosition;

        void Start()
        {
            _particles = new GameObject[popSize];
            _pBestCosts = new float[popSize];
            _pBestPositions = new Vector3[popSize];
            _velocities = new Vector3[popSize];
            _positions = new Vector3[popSize];

            AssignRandomTargetPosition();
            InitPopulation();
            StartCoroutine(RunPso());
        }

        private IEnumerator RunPso()
        {
            while (IsTargetArrived())
            {
                while (_iteration < maxIter)
                {
                    _iteration++;
                    Debug.Log("gBestCost " + _gBestCost);

                    for (int i = 0; i < popSize; i++)
                    {
                        UpdateParticle(i);
                    }

                    yield return new WaitForSeconds(waitTime);
                }

                _robotPos = _gBestPosition;
                _iteration = 0;

                ClearPopulation();

                if (IsTargetArrived())
                {
                    InitPopulation();
                }
            }
        }

        private void UpdateParticle(int i)
        {
            Vector2 vel = Vector3.ClampMagnitude(GetVelocity(_velocities[i], _positions[i], _pBestPositions[i]),
                maxVelocity);
            Vector2 pos = GetPosition(_positions[i], vel);

            pos = ClampPosition(pos);
            var cost = Vector3.Distance(target.position, pos);

            UpdateGlobalBest(cost, pos, i);
            UpdateGlobalBestCost(cost, pos, i);

            _positions[i] = pos;
            _velocities[i] = vel;
            _particles[i].transform.position = pos;
        }

        private Vector2 ClampPosition(Vector2 pos)
        {
            if (pos.x > _xMax)
            {
                pos.x = _xMax;
            }

            if (pos.y > _yMax)
            {
                pos.y = _yMax;
            }

            return pos;
        }


        private Vector3 GetVelocity(Vector3 previousVelocity, Vector3 previousPosition, Vector3 pBest)
        {
            return GetInertia() * previousVelocity + c1 * Random.Range(0f, 1f) * (pBest - previousPosition) +
                   c2 * Random.Range(0f, 1f) * (_gBestPosition - previousPosition);
        }

        private Vector3 GetPosition(Vector3 previousPosition, Vector3 currentVelocity)
        {
            return previousPosition + currentVelocity;
        }

        private float GetInertia()
        {
            return startInertia - (startInertia - endInertia) * _iteration / maxIter;
        }

        private void InitPopulation()
        {
            bool isOverlap = CheckOverlap();
            
            if (isOverlap)
            {
                HandleOverlap();
            }
            else
            {
                direction = "";
                _xMax += 0.5f;
                _yMax += 0.5f;
            }
            
            _gBestCost = float.MaxValue;

            for (int i = 0; i < popSize; i++)
            {
                Vector2 pos = GenerateRandomPosition(isOverlap);

                _particles[i] = Instantiate(particlePrefab, pos, Quaternion.identity);

                float cost = CalculateCost(pos);
                UpdateGlobalBest(cost, pos, i);

                InitializeParticleData(i, pos, cost);
            }
            
        }

        private bool CheckOverlap()
        {
            return Physics2D.OverlapCircle(_gBestPosition, 1);
        }

        private float CalculateCost(Vector2 position)
        {
            return Vector3.Distance(target.position, position);
        }

        private void UpdateGlobalBest(float cost, Vector2 position, int particleIndex)
        {
            if (cost < _gBestCost)
            {
                _gBestCost = cost;
                bestParticle = particleIndex;
                _gBestPosition = position;
            }
        }

        private void UpdateGlobalBestCost(float cost, Vector2 position, int particleIndex)
        {
            if (cost < _pBestCosts[particleIndex])
            {
                _pBestCosts[particleIndex] = cost;
                _pBestPositions[particleIndex] = position;
            }
        }

        private void InitializeParticleData(int i, Vector2 position, float cost)
        {
            _pBestPositions[i] = position;
            _pBestCosts[i] = cost;
            _positions[i] = position;
            _velocities[i] = Vector3.ClampMagnitude(position, maxVelocity);
        }

        public void AssignRandomTargetPosition()
        {
            float randomX = Random.Range(minRandPosition.x, maxRandPosition.x);
            float randomY = Random.Range(minRandPosition.y, maxRandPosition.y);

            Vector2 randomPosition = new Vector2(randomX, randomY);

            target.position = randomPosition;
            _robotPos = transform.position;
            
            _xMax = _robotPos.x;
            _yMax = _robotPos.y;
            _gBestPosition = _robotPos;
        }

        private void ClearPopulation()
        {
            foreach (var t in _particles)
            {
                Destroy(t);
            }
        }

        private bool IsTargetArrived()
        {
            return (Vector3.Distance(_robotPos, target.position) >= 1);
        }

        private void HandleOverlap()
        {
            switch (direction)
            {
                case "" when _firstChange:
                    SetRandomDirection();
                    break;
                case "":
                    SetDirection();
                    break;
                case "h":
                    _xMax += 0.5f;
                    break;
                case "v":
                    _yMax += 0.5f;
                    break;
            }
        }

        private Vector2 GenerateRandomPosition(bool isOverlap)
        {
            return isOverlap ? GenerateParticlePositionFromMaxBounds() : GenerateParticlePosition();
        }

        private Vector2 GenerateParticlePositionFromMaxBounds()
        {
            return new Vector2(Random.Range(_xMax - 1, _xMax),
                Random.Range(_yMax - 1, _yMax));
        }

        private Vector2 GenerateParticlePosition()
        {
            return new Vector2(Random.Range(_robotPos.x - 0.5f, _robotPos.x + 0.5f), 
                Random.Range(_robotPos.y - 0.5f, _robotPos.y + 0.5f));
        }

        void SetDirection()
        {
            Vector2 endCast = new Vector2();
            endCast.y = 35;
            endCast.y = _robotPos.y;
            RaycastHit2D hit =  GetHit(endCast);
            if (hit.collider == null || hit.distance>1)
            {
                direction = "h";
                _xMax += 0.5f;
                return;
            }
            endCast.x = _robotPos.x;
            endCast.y = 15;
            hit = GetHit(endCast);
            if (hit.collider == null || hit.distance > 1)
            {
                direction = "v";
                _yMax += 0.5f;
                return;
            }
        }

        void SetRandomDirection()
        {
            Vector2 endCast = new Vector2();
            RaycastHit2D hit;
            if (Random.Range(0f, 1f) > 0.5f)
            {
                endCast.y = 35;
                endCast.y = _robotPos.y;
                hit = GetHit(endCast);
                if (hit.collider == null || hit.distance > 1)
                {
                    direction = "h";
                    _xMax += 0.5f;
                }
            }
            else
            {
                endCast.x = _robotPos.x;
                endCast.y = 15;
                hit = GetHit(endCast);
                if (hit.collider == null || hit.distance > 1)
                {
                    direction = "v";
                    _yMax += 0.5f;
                }
            }
            _firstChange = false;
        }

        RaycastHit2D GetHit(Vector2 endCast)
        {
            return Physics2D.Linecast(_robotPos, endCast);
        }

    }
}