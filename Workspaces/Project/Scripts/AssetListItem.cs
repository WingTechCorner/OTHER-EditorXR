﻿using System;
using System.Collections;
using System.IO;
using ListView;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VR.Handles;
using UnityEngine.VR.Utilities;
using Object = UnityEngine.Object;

public class AssetListItem : ListViewItem<AssetData>
{
	private const float kMargin = 0.01f;
	private const float kIndent = 0.02f;

	private const float kMagnetizeDuration = 0.75f;
	private readonly Vector3 kGrabOffset = new Vector3(0, 0.02f, 0.03f);

	[SerializeField]
	private Text m_Text;

	[SerializeField]
	private DirectHandle m_Cube;

	[SerializeField]
	private DirectHandle m_ExpandArrow;

	[SerializeField]
	private Material m_NoClipCubeMaterial;

	[SerializeField]
	private Material m_NoClipExpandArrowMaterial;

	private Renderer m_CubeRenderer;
	private bool m_Setup;

	private Transform m_GrabbedObject;
	private float m_GrabLerp;
	
	public override void Setup(AssetData data)
	{
		base.Setup(data);
		//First time setup
		if (!m_Setup) {
			//Cube material might change, so we always instance it
			m_CubeRenderer = m_Cube.GetComponent<Renderer>();
			U.Material.GetMaterialClone(m_CubeRenderer);

			m_ExpandArrow.onHandleEndDrag += ToggleExpanded;
			m_Cube.onHandleBeginDrag += GrabBegin;
			m_Cube.onHandleDrag += GrabDrag;
			m_Cube.onHandleEndDrag += GrabEnd;

			m_Setup = true;
		}

		m_Text.text = Path.GetFileName(data.path);
		if (data.children != null)
		{
			m_ExpandArrow.gameObject.SetActive(true);
		}
		else
		{
			m_ExpandArrow.gameObject.SetActive(false);
		}
	}

	public void SwapMaterials(Material textMaterial, Material expandArrowMaterial)
	{
		m_Text.material = textMaterial;
		m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = expandArrowMaterial;
	}

	public void Resize(float width)
	{
		Vector3 cubeScale = m_Cube.transform.localScale;
		cubeScale.x = width;
		m_Cube.transform.localScale = cubeScale;

		var arrowWidth = m_ExpandArrow.transform.localScale.x * 0.5f;
		var contentHeight = m_ExpandArrow.transform.localPosition.y;
		var halfWidth = width * 0.5f;
		var indent = kIndent * data.treeDepth;
		var doubleMargin = kMargin * 2;
		m_ExpandArrow.transform.localPosition = new Vector3(kMargin + indent - halfWidth, contentHeight, 0);

		m_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width - doubleMargin - indent) * 1 / m_Text.transform.localScale.x);
		m_Text.transform.localPosition = new Vector3(doubleMargin + indent + arrowWidth - halfWidth, contentHeight, 0); //Text is next to arrow, with a margin and indent
	}

	public void GetMaterials(out Material textMaterial, out Material expandArrowMaterial)
	{
		textMaterial = Object.Instantiate(m_Text.material);
		expandArrowMaterial = Object.Instantiate(m_ExpandArrow.GetComponent<Renderer>().sharedMaterial);
	}

	public void Clip(Bounds bounds, Matrix4x4 parentMatrix)
	{
		m_CubeRenderer.sharedMaterial.SetMatrix("_ParentMatrix", parentMatrix);
		m_CubeRenderer.sharedMaterial.SetVector("_ClipExtents",  bounds.extents);
	}

	private void ToggleExpanded(BaseHandle baseHandle, HandleDragEventData handleDragEventData)
	{
		data.expanded = !data.expanded;
	}

	private void GrabBegin(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		var clone = (GameObject)Instantiate(gameObject, transform.position, transform.rotation, transform.parent);
		var cloneItem = clone.GetComponent<AssetListItem>();
		cloneItem.m_Cube.GetComponent<Renderer>().sharedMaterial = m_NoClipCubeMaterial;
		cloneItem.m_ExpandArrow.GetComponent<Renderer>().sharedMaterial = m_NoClipExpandArrowMaterial;
		cloneItem.m_Text.material = null;

		m_GrabbedObject = clone.transform;
		m_GrabLerp = 0;
		StartCoroutine(Magnetize());
	}

	private IEnumerator Magnetize()
	{
		var startTime = Time.realtimeSinceStartup;
		var currTime = 0f;
		while (currTime < kMagnetizeDuration)
		{
			currTime = Time.realtimeSinceStartup - startTime;
			m_GrabLerp = currTime / kMagnetizeDuration;
			yield return null;
		}
		m_GrabLerp = 1;
	}

	private void GrabDrag(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		var rayTransform = eventData.rayOrigin.transform;
		m_GrabbedObject.transform.position = Vector3.Lerp(m_GrabbedObject.transform.position, rayTransform.position + rayTransform.rotation * kGrabOffset, m_GrabLerp);
		m_GrabbedObject.transform.rotation = Quaternion.Lerp(m_GrabbedObject.transform.rotation, rayTransform.rotation, m_GrabLerp);
	}

	private void GrabEnd(BaseHandle baseHandle, HandleDragEventData eventData)
	{
		U.Object.Destroy(m_GrabbedObject.gameObject);
	}

	private void OnDestroy()
	{
		if(m_CubeRenderer)
			U.Object.Destroy(m_CubeRenderer.sharedMaterial);
	}
}