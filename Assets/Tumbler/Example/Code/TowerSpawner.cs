// TUMBLER
// Copyright (c) 2020 Ted Brown

using Jambox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tumbler
{
	public class TowerSpawner : MonoBehaviour 
	{
		public GameObject _blockPrefab;
		public int _blockCount = 54;
		public float _scale = 1;

		private int _activeCount;
		private float _tinyOffset = 0.01f;
		private List<GameObject> _blocks;
		private float _timer = 1;

		private void AddBlock (Vector3 position, Quaternion rotation)
		{
			GameObject block = Instantiate(_blockPrefab, position, rotation);
			block.transform.localScale *= _scale;
			Rigidbody rb = block.GetComponent<Rigidbody>();
			rb.mass = rb.mass * _scale;
			_blocks.Add(block);
			block.SetActive(false);
		}

		protected void Awake ()
		{
			Vector3 size = _blockPrefab.GetComponent<BoxCollider>().size * _scale;
			_tinyOffset *= _scale;
			Vector3 spawnPosition = transform.position + Vector3.up * ((size.y * 0.5f) + _tinyOffset);
			Quaternion spawnRotation = transform.rotation;

			int layerCount = _blockCount / 3;
			_blocks = new List<GameObject>(_blockCount);

			for (int layer = 0; layer < layerCount; layer++)
			{
				// spawn to the center, left, and right
				AddBlock(spawnPosition, spawnRotation);
				AddBlock(spawnPosition + spawnRotation * Vector3.right * (size.x + _tinyOffset), spawnRotation);
				AddBlock(spawnPosition + spawnRotation * Vector3.left * (size.x + _tinyOffset), spawnRotation);
				spawnPosition += Vector3.up * (size.y + _tinyOffset);
				spawnRotation *= Quaternion.Euler(Vector3.up * 90);
			}
		}

		protected void Update ()
		{
			_timer -= Time.deltaTime;
			if (_timer < 0)
			{
				if (_activeCount < _blocks.Count)
				{
					_blocks[_activeCount].SetActive(true);
					_activeCount++;
				}
				_timer = 1;
			}
		}
	}
}
