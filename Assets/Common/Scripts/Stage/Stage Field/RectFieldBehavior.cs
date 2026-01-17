using OctoberStudio.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace OctoberStudio
{
    public class RectFieldBehavior : AbstractFieldBehavior
    {
        readonly List<StageChunkBehavior> chunks = new List<StageChunkBehavior>();
        readonly List<Transform> borders = new List<Transform>();

        // cached bounds
        float leftBound, rightBound, topBound, bottomBound;
        Vector2 chunkSize;

        public override void Init(StageFieldData stageFieldData, bool spawnProp)
        {
            base.Init(stageFieldData, spawnProp);

            int cols = Mathf.Max(1, stageFieldData.RectColumns);
            int rows = Mathf.Max(1, stageFieldData.RectRows);

            // Spawn first chunk to get size
            var first = Object.Instantiate(stageFieldData.GetBackgroundPrefabs().Random())
                .GetComponent<StageChunkBehavior>();

            first.transform.position = Vector3.zero;
            first.transform.rotation = Quaternion.identity;
            first.transform.localScale = Vector3.one;

            chunkSize = first.Size;
            chunks.Add(first);

            // Spawn remaining chunks in a centered grid
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (x == 0 && y == 0) continue;

                    var chunk = Object.Instantiate(stageFieldData.GetBackgroundPrefabs().Random())
                        .GetComponent<StageChunkBehavior>();

                    chunk.transform.rotation = Quaternion.identity;
                    chunk.transform.localScale = Vector3.one;

                    chunks.Add(chunk);
                }
            }

            // Position all chunks (centered around 0,0)
            int index = 0;
            float xCenter = (cols - 1) / 2f;
            float yCenter = (rows - 1) / 2f;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var chunk = chunks[index++];

                    float px = (x - xCenter) * chunkSize.x;
                    float py = (y - yCenter) * chunkSize.y;

                    chunk.transform.position = new Vector3(px, py, 0);

                    SpawnProp(chunk);
                }
            }

            // Calculate total field bounds
            float halfW = cols * chunkSize.x / 2f;
            float halfH = rows * chunkSize.y / 2f;

            leftBound = -halfW;
            rightBound = halfW;
            bottomBound = -halfH;
            topBound = halfH;

            // Borders are placed outside by half chunk (same logic as old single-chunk: border at +/- chunk.Size)
            float borderX = halfW + chunkSize.x / 2f;
            float borderY = halfH + chunkSize.y / 2f;

            // --- NEW: tile borders along the whole perimeter (no gaps) ---
            // Top/Bottom: one segment per column
            for (int x = 0; x < cols; x++)
            {
                float px = (x - xCenter) * chunkSize.x;

                if (stageFieldData.TopPrefab != null)
                {
                    var t = Object.Instantiate(stageFieldData.TopPrefab).transform;
                    t.position = new Vector3(px, borderY, 0);
                    borders.Add(t);
                }

                if (stageFieldData.BottomPrefab != null)
                {
                    var b = Object.Instantiate(stageFieldData.BottomPrefab).transform;
                    b.position = new Vector3(px, -borderY, 0);
                    borders.Add(b);
                }
            }

            // Left/Right: one segment per row
            for (int y = 0; y < rows; y++)
            {
                float py = (y - yCenter) * chunkSize.y;

                if (stageFieldData.LeftPrefab != null)
                {
                    var l = Object.Instantiate(stageFieldData.LeftPrefab).transform;
                    l.position = new Vector3(-borderX, py, 0);
                    borders.Add(l);
                }

                if (stageFieldData.RightPrefab != null)
                {
                    var r = Object.Instantiate(stageFieldData.RightPrefab).transform;
                    r.position = new Vector3(borderX, py, 0);
                    borders.Add(r);
                }
            }

            // Corners (4 pieces)
            if (stageFieldData.TopLeftPrefab != null)
            {
                var c = Object.Instantiate(stageFieldData.TopLeftPrefab).transform;
                c.position = new Vector3(-borderX, borderY, 0);
                borders.Add(c);
            }

            if (stageFieldData.TopRightPrefab != null)
            {
                var c = Object.Instantiate(stageFieldData.TopRightPrefab).transform;
                c.position = new Vector3(borderX, borderY, 0);
                borders.Add(c);
            }

            if (stageFieldData.BottomLeftPrefab != null)
            {
                var c = Object.Instantiate(stageFieldData.BottomLeftPrefab).transform;
                c.position = new Vector3(-borderX, -borderY, 0);
                borders.Add(c);
            }

            if (stageFieldData.BottomRightPrefab != null)
            {
                var c = Object.Instantiate(stageFieldData.BottomRightPrefab).transform;
                c.position = new Vector3(borderX, -borderY, 0);
                borders.Add(c);
            }
        }

        public override void Update()
        {
            // Rect field is static
        }

        public override bool ValidatePosition(Vector2 position)
        {
            if (position.x > rightBound) return false;
            if (position.x < leftBound) return false;
            if (position.y > topBound) return false;
            if (position.y < bottomBound) return false;
            return true;
        }

        public override Vector2 GetRandomPositionOnBorder()
        {
            // Pick random side and point on that edge
            int side = Random.Range(0, 4); // 0=top 1=bottom 2=left 3=right
            switch (side)
            {
                case 0: return new Vector2(Random.Range(leftBound, rightBound), topBound);
                case 1: return new Vector2(Random.Range(leftBound, rightBound), bottomBound);
                case 2: return new Vector2(leftBound, Random.Range(bottomBound, topBound));
                default: return new Vector2(rightBound, Random.Range(bottomBound, topBound));
            }
        }

        public override Vector2 GetBossSpawnPosition(BossFenceBehavior fence, Vector2 offset)
        {
            var playerPosition = PlayerBehavior.Player.transform.position.XY();
            var desired = playerPosition + offset;

            // clamp into field (with fence size)
            if (fence is CircleFenceBehavior c)
            {
                desired.x = Mathf.Clamp(desired.x, leftBound + c.Radius, rightBound - c.Radius);
                desired.y = Mathf.Clamp(desired.y, bottomBound + c.Radius, topBound - c.Radius);
            }
            else if (fence is RectFenceBehavior r)
            {
                desired.x = Mathf.Clamp(desired.x, leftBound + r.Width / 2f, rightBound - r.Width / 2f);
                desired.y = Mathf.Clamp(desired.y, bottomBound + r.Height / 2f, topBound - r.Height / 2f);
            }

            return desired;
        }

        public override bool IsPointOutsideRight(Vector2 point, out float distance)
        {
            bool result = point.x > rightBound;
            distance = result ? point.x - rightBound : 0;
            return result;
        }

        public override bool IsPointOutsideLeft(Vector2 point, out float distance)
        {
            bool result = point.x < leftBound;
            distance = result ? leftBound - point.x : 0;
            return result;
        }

        public override bool IsPointOutsideTop(Vector2 point, out float distance)
        {
            bool result = point.y > topBound;
            distance = result ? point.y - topBound : 0;
            return result;
        }

        public override bool IsPointOutsideBottom(Vector2 point, out float distance)
        {
            bool result = point.y < bottomBound;
            distance = result ? bottomBound - point.y : 0;
            return result;
        }

        public override Vector2 GetIntersectionPoint(Vector2 start, Vector2 end, float offset)
        {
            var path = end - start;
            if (path.sqrMagnitude < Mathf.Epsilon) return start;

            var rect = new Rect(
                leftBound + offset,
                bottomBound + offset,
                (rightBound - leftBound) - offset * 2f,
                (topBound - bottomBound) - offset * 2f
            );

            float tx1, tx2, ty1, ty2;

            if (Mathf.Abs(path.x) > Mathf.Epsilon)
            {
                tx1 = (rect.xMin - start.x) / path.x;
                tx2 = (rect.xMax - start.x) / path.x;
                if (tx1 > tx2) (tx1, tx2) = (tx2, tx1);
            }
            else
            {
                tx1 = float.NegativeInfinity;
                tx2 = float.PositiveInfinity;
            }

            if (Mathf.Abs(path.y) > Mathf.Epsilon)
            {
                ty1 = (rect.yMin - start.y) / path.y;
                ty2 = (rect.yMax - start.y) / path.y;
                if (ty1 > ty2) (ty1, ty2) = (ty2, ty1);
            }
            else
            {
                ty1 = float.NegativeInfinity;
                ty2 = float.PositiveInfinity;
            }

            var tEnter = Mathf.Max(tx1, ty1);
            var tExit = Mathf.Min(tx2, ty2);

            if (tExit < 0f || tEnter > tExit) return start;

            return start + path * tExit;
        }

        public override void RemovePropFromBossFence(BossFenceBehavior fence)
        {
            for (int i = 0; i < chunks.Count; i++)
                chunks[i].RemovePropFromBossFence(fence);
        }

        public override void Clear()
        {
            for (int i = 0; i < chunks.Count; i++)
                Object.Destroy(chunks[i].gameObject);
            chunks.Clear();

            for (int i = 0; i < borders.Count; i++)
                Object.Destroy(borders[i].gameObject);
            borders.Clear();
        }
    }
}
