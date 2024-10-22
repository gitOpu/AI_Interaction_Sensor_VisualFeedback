using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FieldOfView
{
	public class HunterFieldOfView : FovAsst
{

	public float viewRadius = 3.0f;
	public float attackRadius = 1.0f;

	[Range(0, 360)]
	public float viewAngle = 60;

	public LayerMask targetMask;
	public LayerMask obstacleMask;

	//[HideInInspector]
	public List<Transform> visibleTargets = new List<Transform>();
	public List<GameObject> victimUnderAttackRange = new List<GameObject>();

	public float meshResolution = 1f;
	public int edgeResolveIterations = 4;
	public float edgeDstThreshold = 0.5f;

	public MeshFilter viewMeshFilter;
	public MeshRenderer meshRenderer;
	public Color meshRendererColor;
	Mesh viewMesh;

    private void Awake()
    {
		viewMesh = new Mesh();
		viewMesh.name = "View Mesh";
		viewMeshFilter.mesh = viewMesh;
	}
    void Start()
	{
		StartCoroutine("FindTargetsWithDelay", 0.05f);
		//DrawFieldOfView();
		meshRenderer.material.color = meshRendererColor;
	}


	IEnumerator FindTargetsWithDelay(float delay)
	{
		while (true)
		{
			yield return new WaitForSeconds(delay);
			FindVisibleTargets();
		}
	}

	void LateUpdate()
	{
		if (true) DrawFieldOfView(); else viewMesh.Clear();
	}

	void FindVisibleTargets()
	{
		visibleTargets.Clear();
		
		Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

		for (int i = 0; i < targetsInViewRadius.Length; i++)
		{
			Transform target = targetsInViewRadius[i].transform;
			Vector3 dirToTarget = (target.position - transform.position).normalized;

			float dstToTarget = Vector3.Distance(transform.position, target.position);

			if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2 || dstToTarget <= attackRadius)
			{

				if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
				{

					visibleTargets.Add(target);
					if (dstToTarget < attackRadius)
					{
						if ( victimUnderAttackRange.Count == 0)
						{
							 victimUnderAttackRange.Add(target.gameObject);
								
						}
					}
				}
			}
		}
	}

	void DrawFieldOfView()
	{
		float fullCircle = 360;
		int stepCount = Mathf.RoundToInt(fullCircle * meshResolution);
		float stepAngleSize = fullCircle / stepCount;

		List<Vector3> viewPoints = new List<Vector3>();
		ViewCastInfo oldViewCast = new ViewCastInfo();
		for (int i = 0; i <= stepCount; i++)
		{
			float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i; //(transform.eulerAngles.y + stepAngleSize * i) ;

			//Debug.Log($"angle {angle}");



			float currentRadius = 0;

			if (angle - viewAngle / 2 < transform.eulerAngles.y)
			{
				currentRadius = viewRadius;
			}
			else
			{
				currentRadius = attackRadius;
			}

			ViewCastInfo newViewCast = ViewCast(angle, currentRadius, obstacleMask);

			if (i > 0)
			{
				bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
				if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
				{
					 
					EdgeInfo edge = FindEdge(oldViewCast, newViewCast, currentRadius, edgeResolveIterations, edgeDstThreshold, obstacleMask);
					if (edge.pointA != Vector3.zero)
					{
						viewPoints.Add(edge.pointA);
					}
					if (edge.pointB != Vector3.zero)
					{
						viewPoints.Add(edge.pointB);
					}
				}

			}


			viewPoints.Add(newViewCast.point);
			oldViewCast = newViewCast;
		}

		int vertexCount = viewPoints.Count + 1;
		Vector3[] vertices = new Vector3[vertexCount];
		int[] triangles = new int[(vertexCount - 2) * 3];

		vertices[0] = Vector3.zero;
		for (int i = 0; i < vertexCount - 1; i++)
		{
			vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

			if (i < vertexCount - 2)
			{
				triangles[i * 3] = 0;
				triangles[i * 3 + 1] = i + 1;
				triangles[i * 3 + 2] = i + 2;
			}
		}

		viewMesh.Clear();

		viewMesh.vertices = vertices;
		viewMesh.triangles = triangles;
		viewMesh.RecalculateNormals();
	}



}
}
