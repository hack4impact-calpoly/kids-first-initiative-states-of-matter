using UnityEngine;

public partial class StateChangeCutsceneAnimation
{
    private Vector2 PositionOnEllipse(float t, float halfWidth, float halfHeight)
    {
        float angle = t * Mathf.PI * 2f;
        return new Vector2(Mathf.Cos(angle) * halfWidth, Mathf.Sin(angle) * halfHeight);
    }

    private Vector2 BounceInside(Vector2 position, ref Vector2 velocity)
    {
        float maxX = particleAreaSize.x * 0.45f;
        float maxY = particleAreaSize.y * 0.36f;

        if (position.x < -maxX || position.x > maxX)
        {
            velocity.x *= -1f;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
        }

        if (position.y < -maxY || position.y > maxY)
        {
            velocity.y *= -1f;
            position.y = Mathf.Clamp(position.y, -maxY, maxY);
        }

        return position;
    }

    private Vector2 BounceInsideContainer(Vector2 position, ref Vector2 velocity)
    {
        return BounceInsideContainer(position, ref velocity, 0f);
    }

    private Vector2 BounceInsideContainer(Vector2 position, ref Vector2 velocity, float margin)
    {
        float minY = -particleAreaSize.y * 0.4f + margin;
        float maxY = particleAreaSize.y * 0.4f - margin;

        if (position.y < minY || position.y > maxY)
        {
            velocity.y *= -1f;
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        float maxX = Mathf.Max(0f, GetContainerHalfWidth(position.y) - margin);
        if (position.x < -maxX || position.x > maxX)
        {
            velocity.x *= -1f;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
        }

        return position;
    }

    private Vector2 ClampInsideContainer(Vector2 position, float margin)
    {
        float minY = -particleAreaSize.y * 0.4f + margin;
        float maxY = particleAreaSize.y * 0.4f - margin;
        position.y = Mathf.Clamp(position.y, minY, maxY);

        float maxX = Mathf.Max(0f, GetContainerHalfWidth(position.y) - margin);
        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        return position;
    }

    private Vector2 BounceInsideWaxLiquid(Vector2 position, ref Vector2 velocity, float margin)
    {
        float minY = -particleAreaSize.y * 0.36f + margin;
        float maxY = particleAreaSize.y * 0.16f - margin;

        if (position.y < minY || position.y > maxY)
        {
            velocity.y *= -1f;
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        float maxX = Mathf.Max(0f, GetContainerHalfWidth(position.y) - margin);
        if (position.x < -maxX || position.x > maxX)
        {
            velocity.x *= -1f;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
        }

        return position;
    }

    private Vector2 BounceInsidePlasmaTube(Vector2 position, ref Vector2 velocity, float margin)
    {
        float maxX = particleAreaSize.x * 0.39f - margin;
        float maxY = particleAreaSize.y * 0.2f - margin;

        if (position.x < -maxX || position.x > maxX)
        {
            velocity.x *= -1f;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
        }

        if (position.y < -maxY || position.y > maxY)
        {
            velocity.y *= -1f;
            position.y = Mathf.Clamp(position.y, -maxY, maxY);
        }

        return position;
    }

    private Vector2 ClampInsidePlasmaTube(Vector2 position, float margin)
    {
        float maxX = particleAreaSize.x * 0.39f - margin;
        float maxY = particleAreaSize.y * 0.2f - margin;

        position.x = Mathf.Clamp(position.x, -maxX, maxX);
        position.y = Mathf.Clamp(position.y, -maxY, maxY);
        return position;
    }

    private float GetContainerHalfWidth(float y)
    {
        float bodyHalfWidth = particleAreaSize.x * 0.115f;
        float neckHalfWidth = particleAreaSize.x * 0.06f;
        float shoulderStartY = particleAreaSize.y * 0.13f;
        float shoulderEndY = particleAreaSize.y * 0.3f;

        if (y <= shoulderStartY)
            return bodyHalfWidth;

        if (y >= shoulderEndY)
            return neckHalfWidth;

        float shoulderProgress = Mathf.InverseLerp(shoulderStartY, shoulderEndY, y);
        return Mathf.Lerp(bodyHalfWidth, neckHalfWidth, shoulderProgress);
    }
}
