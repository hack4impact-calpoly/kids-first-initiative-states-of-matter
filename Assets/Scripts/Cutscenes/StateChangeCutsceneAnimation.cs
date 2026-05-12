using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MatterCutsceneKind
{
    ChocolateMelting,
    LiquidFlow,
    LiquidFreezing,
    PipeWaterFlow,
    PipeFreezing,
    CircuitEnergy
}

public class StateChangeCutsceneAnimation : MonoBehaviour, ICutsceneAnimation, ICutsceneAnimationCleanup
{
    [SerializeField] private MatterCutsceneKind cutsceneKind = MatterCutsceneKind.LiquidFreezing;
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float particleSize = 34f;
    [SerializeField] private Vector2 particleAreaSize = new Vector2(880f, 430f);
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private float firstStageDuration = 1.7f;
    [SerializeField] private float secondStageDuration = 2.2f;
    [SerializeField] private float finalStageDuration = 1.2f;

    private const int RandomSeed = 2749;
    private static Sprite circleSprite;
    private CutsceneView activeView;

    public void Configure(MatterCutsceneKind kind)
    {
        cutsceneKind = kind;
    }

    public IEnumerator Play(CutsceneContext context)
    {
        if (context == null || context.OverlayRoot == null)
            yield break;

        Cleanup(context);

        CutsceneView view = BuildView(context.OverlayRoot);
        activeView = view;

        try
        {
            ApplyText(view, 0);
            TickStage(view, 0, 0f, 0f);

            yield return Fade(view.Group, 0f, 1f, fadeDuration, context);
            yield return AnimateStage(view, firstStageDuration, 0, context);

            ApplyText(view, 1);
            yield return AnimateStage(view, secondStageDuration, 1, context);

            ApplyText(view, 2);
            yield return AnimateStage(view, finalStageDuration, 2, context);
            yield return Fade(view.Group, 1f, 0f, fadeDuration, context);
        }
        finally
        {
            DestroyView(view);
        }
    }

    public void Cleanup(CutsceneContext context)
    {
        if (activeView != null)
            DestroyView(activeView);
    }

    private void DestroyView(CutsceneView view)
    {
        if (view == null)
            return;

        if (view.Root != null)
            Destroy(view.Root.gameObject);

        if (activeView == view)
            activeView = null;
    }

    private CutsceneView BuildView(RectTransform parent)
    {
        EnsureCircleSprite();

        var rootObject = new GameObject("State Change Cutscene", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        rootObject.transform.SetParent(parent, false);

        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        CanvasGroup group = rootObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        Image backdrop = rootObject.GetComponent<Image>();
        backdrop.color = GetBackdropColor();
        backdrop.raycastTarget = false;

        RectTransform panel = CreateRect("Matter View", root, new Vector2(1040f, 640f), Vector2.zero);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = GetPanelColor();
        panelImage.raycastTarget = false;

        TextMeshProUGUI title = CreateLabel("Title", panel, new Vector2(960f, 72f), new Vector2(0f, 260f), 42f);
        title.color = GetTitleColor();

        TextMeshProUGUI stageLabel = CreateLabel("Stage Label", panel, new Vector2(960f, 82f), new Vector2(0f, -276f), 29f);
        stageLabel.color = GetLabelColor();

        RectTransform particleArea = CreateRect("Particle Area", panel, particleAreaSize, new Vector2(0f, -16f));
        List<ParticleView> particles = CreateParticles(particleArea);
        List<BondView> bonds = CreateBonds(particleArea, particles);
        List<RectTransform> flowLines = CreateFlowLines(particleArea);
        IceCubeView iceCube = CreateIceCube(particleArea);
        ContainerView container = CreateContainer(particleArea);

        return new CutsceneView(root, group, title, stageLabel, particleArea, particles, bonds, flowLines, iceCube, container);
    }

    private List<ParticleView> CreateParticles(RectTransform parent)
    {
        int count = Mathf.Max(1, particleCount);
        var particles = new List<ParticleView>(count);
        var random = new System.Random(RandomSeed + (int)cutsceneKind);
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count * 1.6f));
        int rows = Mathf.CeilToInt(count / (float)columns);
        float spacingX = particleAreaSize.x / (columns + 1f);
        float spacingY = particleAreaSize.y / (rows + 1f);

        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Vector2 solidPosition = new Vector2(
                -particleAreaSize.x * 0.5f + spacingX * (column + 1f),
                particleAreaSize.y * 0.5f - spacingY * (row + 1f));

            Vector2 liquidPosition = new Vector2(
                Mathf.Lerp(-particleAreaSize.x * 0.42f, particleAreaSize.x * 0.42f, (float)random.NextDouble()),
                Mathf.Lerp(-particleAreaSize.y * 0.34f, particleAreaSize.y * 0.34f, (float)random.NextDouble()));

            Vector2 velocity = RandomDirection(random) * Mathf.Lerp(75f, 150f, (float)random.NextDouble());
            RectTransform rect = CreateRect($"Particle {i + 1}", parent, Vector2.one * particleSize, liquidPosition);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = circleSprite;
            image.color = GetParticleColor(i);
            image.raycastTarget = false;

            particles.Add(new ParticleView(rect, image, solidPosition, liquidPosition, velocity, Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble())));
        }

        return particles;
    }

    private List<BondView> CreateBonds(RectTransform parent, IReadOnlyList<ParticleView> particles)
    {
        var bonds = new List<BondView>();
        int columns = Mathf.CeilToInt(Mathf.Sqrt(particles.Count * 1.6f));

        for (int i = 0; i < particles.Count - 1; i++)
        {
            bool sameRow = Mathf.Abs(particles[i].SolidPosition.y - particles[i + 1].SolidPosition.y) < particleSize;
            if (sameRow)
                bonds.Add(CreateBond(parent, particles[i], particles[i + 1]));
        }

        for (int i = 0; i + columns < particles.Count; i++)
            bonds.Add(CreateBond(parent, particles[i], particles[i + columns]));

        return bonds;
    }

    private BondView CreateBond(RectTransform parent, ParticleView start, ParticleView end)
    {
        RectTransform rect = CreateRect("Particle Bond", parent, new Vector2(10f, 4f), Vector2.zero);
        rect.SetAsFirstSibling();

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.72f, 0.92f, 1f, 0f);
        image.raycastTarget = false;
        return new BondView(rect, image, start, end);
    }

    private List<RectTransform> CreateFlowLines(RectTransform parent)
    {
        var lines = new List<RectTransform>();
        for (int i = 0; i < 3; i++)
        {
            RectTransform line = CreateRect($"Flow Line {i + 1}", parent, new Vector2(particleAreaSize.x * 0.82f, 7f), new Vector2(0f, -95f + i * 95f));
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(0.35f, 0.83f, 1f, 0.18f);
            image.raycastTarget = false;
            lines.Add(line);
        }

        return lines;
    }

    private IceCubeView CreateIceCube(RectTransform parent)
    {
        Vector2 size = new Vector2(particleAreaSize.x * 0.8f, particleAreaSize.y * 0.82f);
        RectTransform fill = CreateRect("Ice Cube Fill", parent, size, Vector2.zero);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.55f, 0.9f, 1f, 0f);
        fillImage.raycastTarget = false;
        fill.SetAsFirstSibling();

        float thickness = 10f;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        var edges = new List<Image>(4)
        {
            CreateIceCubeEdge(parent, "Ice Cube Top Edge", new Vector2(size.x, thickness), new Vector2(0f, halfHeight)),
            CreateIceCubeEdge(parent, "Ice Cube Bottom Edge", new Vector2(size.x, thickness), new Vector2(0f, -halfHeight)),
            CreateIceCubeEdge(parent, "Ice Cube Left Edge", new Vector2(thickness, size.y), new Vector2(-halfWidth, 0f)),
            CreateIceCubeEdge(parent, "Ice Cube Right Edge", new Vector2(thickness, size.y), new Vector2(halfWidth, 0f))
        };

        return new IceCubeView(fillImage, edges);
    }

    private Image CreateIceCubeEdge(RectTransform parent, string objectName, Vector2 size, Vector2 position)
    {
        RectTransform edge = CreateRect(objectName, parent, size, position);
        Image image = edge.gameObject.AddComponent<Image>();
        image.color = new Color(0.84f, 0.98f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private ContainerView CreateContainer(RectTransform parent)
    {
        float bodyWidth = particleAreaSize.x * 0.23f;
        float neckWidth = particleAreaSize.x * 0.12f;
        float bottomY = -particleAreaSize.y * 0.43f;
        float bodyTopY = particleAreaSize.y * 0.13f;
        float neckBottomY = particleAreaSize.y * 0.3f;
        float neckTopY = particleAreaSize.y * 0.43f;
        float thickness = 9f;

        float bodyHalfWidth = bodyWidth * 0.5f;
        float neckHalfWidth = neckWidth * 0.5f;
        Vector2 leftShoulderStart = new Vector2(-bodyHalfWidth, bodyTopY);
        Vector2 leftShoulderEnd = new Vector2(-neckHalfWidth, neckBottomY);
        Vector2 rightShoulderStart = new Vector2(bodyHalfWidth, bodyTopY);
        Vector2 rightShoulderEnd = new Vector2(neckHalfWidth, neckBottomY);

        var edges = new List<Image>(7)
        {
            CreateContainerEdge(parent, "Container Bottom", new Vector2(bodyWidth, thickness), new Vector2(0f, bottomY), 0f),
            CreateContainerEdge(parent, "Container Left Wall", new Vector2(thickness, bodyTopY - bottomY), new Vector2(-bodyHalfWidth, (bottomY + bodyTopY) * 0.5f), 0f),
            CreateContainerEdge(parent, "Container Right Wall", new Vector2(thickness, bodyTopY - bottomY), new Vector2(bodyHalfWidth, (bottomY + bodyTopY) * 0.5f), 0f),
            CreateContainerEdgeBetween(parent, "Container Left Shoulder", leftShoulderStart, leftShoulderEnd, thickness),
            CreateContainerEdgeBetween(parent, "Container Right Shoulder", rightShoulderStart, rightShoulderEnd, thickness),
            CreateContainerEdge(parent, "Container Left Neck", new Vector2(thickness, neckTopY - neckBottomY), new Vector2(-neckHalfWidth, (neckBottomY + neckTopY) * 0.5f), 0f),
            CreateContainerEdge(parent, "Container Right Neck", new Vector2(thickness, neckTopY - neckBottomY), new Vector2(neckHalfWidth, (neckBottomY + neckTopY) * 0.5f), 0f)
        };

        Image lip = CreateContainerEdge(parent, "Container Lip", new Vector2(neckWidth * 1.15f, thickness), new Vector2(0f, neckTopY), 0f);
        edges.Add(lip);
        return new ContainerView(edges);
    }

    private Image CreateContainerEdgeBetween(RectTransform parent, string objectName, Vector2 start, Vector2 end, float thickness)
    {
        Vector2 delta = end - start;
        Vector2 size = new Vector2(delta.magnitude, thickness);
        Vector2 position = (start + end) * 0.5f;
        float rotation = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        return CreateContainerEdge(parent, objectName, size, position, rotation);
    }

    private Image CreateContainerEdge(RectTransform parent, string objectName, Vector2 size, Vector2 position, float rotation)
    {
        RectTransform edge = CreateRect(objectName, parent, size, position);
        edge.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = edge.gameObject.AddComponent<Image>();
        image.color = new Color(0.78f, 0.96f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator AnimateStage(CutsceneView view, float duration, int stage, CutsceneContext context)
    {
        CaptureStageStartPositions(view);
        yield return AnimateFor(duration, context, (progress, elapsed, deltaTime) =>
        {
            view.ElapsedTime += deltaTime;
            TickStage(view, stage, progress, deltaTime);
        });
    }

    private void TickStage(CutsceneView view, int stage, float progress, float deltaTime)
    {
        switch (cutsceneKind)
        {
            case MatterCutsceneKind.ChocolateMelting:
                AnimateChocolateMelting(view, stage, progress, view.ElapsedTime, deltaTime);
                break;
            case MatterCutsceneKind.LiquidFreezing:
                AnimateFreezing(view, stage, progress, view.ElapsedTime, deltaTime);
                break;
            case MatterCutsceneKind.PipeWaterFlow:
                AnimatePipeFlow(view, stage, progress, view.ElapsedTime, deltaTime, false);
                break;
            case MatterCutsceneKind.PipeFreezing:
                AnimatePipeFlow(view, stage, progress, view.ElapsedTime, deltaTime, true);
                break;
            case MatterCutsceneKind.CircuitEnergy:
                AnimateCircuit(view, stage, progress, view.ElapsedTime, deltaTime);
                break;
            default:
                AnimateLiquid(view, stage, progress, view.ElapsedTime, deltaTime);
                break;
        }
    }

    private void CaptureStageStartPositions(CutsceneView view)
    {
        for (int i = 0; i < view.Particles.Count; i++)
            view.Particles[i].StageStartPosition = view.Particles[i].Position;
    }

    private void AnimateLiquid(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
    {
        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];
            particle.Position += particle.Velocity * deltaTime;
            particle.Position = BounceInsideContainer(particle.Position, ref particle.Velocity);
            particle.Rect.anchoredPosition = particle.Position + Vector2.up * Mathf.Sin(elapsed * 3.2f + particle.Phase) * 10f;
        }

        UpdateBonds(view.Bonds, 0f);
        SetFlowLineAlpha(view.FlowLines, 0f);
        SetContainerAlpha(view.Container, stage == 0 ? progress : 1f);
    }

    private void AnimateChocolateMelting(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
    {
        float meltAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
        float motion = Mathf.Lerp(0.08f, 1f, meltAmount);

        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];

            if (stage == 2)
            {
                particle.Position += particle.Velocity * deltaTime;
                particle.Position = BounceInside(particle.Position, ref particle.Velocity);
            }
            else
            {
                particle.Position = Vector2.Lerp(particle.SolidPosition, particle.LiquidPosition, meltAmount);
            }

            Vector2 vibration = new Vector2(Mathf.Sin(elapsed * Mathf.Lerp(15f, 28f, motion) + particle.Phase), Mathf.Cos(elapsed * 13f + particle.Phase)) * Mathf.Lerp(5f, 22f, motion);
            particle.Rect.anchoredPosition = particle.Position + vibration;
        }

        UpdateBonds(view.Bonds, 1f - meltAmount);
        SetFlowLineAlpha(view.FlowLines, Mathf.Lerp(0.08f, 0.35f, meltAmount));
    }

    private void AnimateFreezing(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
    {
        float lockAmount = stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f;
        float motion = Mathf.Lerp(1f, 0.08f, lockAmount);

        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];
            if (stage == 0)
            {
                particle.Position += particle.Velocity * deltaTime;
                particle.Position = BounceInside(particle.Position, ref particle.Velocity);
            }
            else
            {
                particle.Position = Vector2.Lerp(particle.StageStartPosition, particle.SolidPosition, lockAmount);
            }

            Vector2 vibration = new Vector2(Mathf.Sin(elapsed * 15f + particle.Phase), Mathf.Cos(elapsed * 13f + particle.Phase)) * (5f * motion);
            particle.Rect.anchoredPosition = particle.Position + vibration;
        }

        UpdateBonds(view.Bonds, lockAmount);
        SetFlowLineAlpha(view.FlowLines, 0f);
        SetIceCubeAlpha(view.IceCube, lockAmount);
    }

    private void AnimatePipeFlow(CutsceneView view, int stage, float progress, float elapsed, float deltaTime, bool freezes)
    {
        float freezeAmount = freezes ? (stage == 0 ? 0f : stage == 1 ? Mathf.SmoothStep(0f, 1f, progress) : 1f) : 0f;
        float speed = Mathf.Lerp(180f, 12f, freezeAmount);
        float halfWidth = particleAreaSize.x * 0.45f;

        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];
            float lane = -120f + (i % 5) * 60f;
            float x = Mathf.Repeat(particle.LiquidPosition.x + elapsed * speed + i * 43f, halfWidth * 2f) - halfWidth;
            Vector2 flowing = new Vector2(x, lane + Mathf.Sin(elapsed * 6f + particle.Phase) * Mathf.Lerp(13f, 1.5f, freezeAmount));
            Vector2 frozen = particle.SolidPosition;
            particle.Position = Vector2.Lerp(flowing, frozen, freezeAmount);
            particle.Rect.anchoredPosition = particle.Position;
        }

        UpdateBonds(view.Bonds, freezeAmount);
        SetFlowLineAlpha(view.FlowLines, Mathf.Lerp(0.38f, 0.08f, freezeAmount));
    }

    private void AnimateCircuit(CutsceneView view, int stage, float progress, float elapsed, float deltaTime)
    {
        float speed = stage == 0 ? 0.45f : 0.95f;
        float energy = stage == 0 ? progress : 1f;
        float halfWidth = particleAreaSize.x * 0.42f;
        float halfHeight = particleAreaSize.y * 0.34f;

        for (int i = 0; i < view.Particles.Count; i++)
        {
            ParticleView particle = view.Particles[i];
            float t = Mathf.Repeat(elapsed * speed + i / (float)view.Particles.Count, 1f);
            particle.Position = PositionOnCircuit(t, halfWidth, halfHeight);
            particle.Rect.anchoredPosition = particle.Position;
            particle.Image.color = Color.Lerp(new Color(0.35f, 0.85f, 1f, 1f), new Color(1f, 0.95f, 0.25f, 1f), energy);
            particle.Rect.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.22f + Mathf.Sin(elapsed * 12f + particle.Phase) * 0.08f, energy);
        }

        UpdateBonds(view.Bonds, 0f);
        SetFlowLineAlpha(view.FlowLines, 0.18f + energy * 0.35f);
    }

    private Vector2 PositionOnCircuit(float t, float halfWidth, float halfHeight)
    {
        float side = t * 4f;
        if (side < 1f)
            return new Vector2(Mathf.Lerp(-halfWidth, halfWidth, side), halfHeight);
        if (side < 2f)
            return new Vector2(halfWidth, Mathf.Lerp(halfHeight, -halfHeight, side - 1f));
        if (side < 3f)
            return new Vector2(Mathf.Lerp(halfWidth, -halfWidth, side - 2f), -halfHeight);

        return new Vector2(-halfWidth, Mathf.Lerp(-halfHeight, halfHeight, side - 3f));
    }

    private void ApplyText(CutsceneView view, int stage)
    {
        string title;
        string label;

        switch (cutsceneKind)
        {
            case MatterCutsceneKind.ChocolateMelting:
                title = "Chocolate Melting";
                label = stage == 0 ? "Solid chocolate particles vibrate in place." :
                    stage == 1 ? "Heating adds energy, so bonds loosen as chocolate melts." :
                    "Melted chocolate particles slide past one another.";
                break;
            case MatterCutsceneKind.LiquidFreezing:
                title = "Liquid to Solid";
                label = stage == 0 ? "Liquid particles slide past each other." :
                    stage == 1 ? "Cooling removes energy, so particles slow down." :
                    "Frozen particles lock into fixed positions and only vibrate.";
                break;
            case MatterCutsceneKind.PipeWaterFlow:
                title = "Liquid Flow";
                label = stage == 0 ? "Liquid particles stay close together." :
                    stage == 1 ? "They flow through connected pipes and take the pipe shape." :
                    "A complete path lets water reach the end.";
                break;
            case MatterCutsceneKind.PipeFreezing:
                title = "Freezing Flow";
                label = stage == 0 ? "Water particles move through open pipe paths." :
                    stage == 1 ? "Freezing removes energy and stops unwanted flow." :
                    "Frozen water holds its shape as a solid barrier.";
                break;
            case MatterCutsceneKind.CircuitEnergy:
                title = "Energy Transfer";
                label = stage == 0 ? "A complete circuit lets electrical energy move." :
                    stage == 1 ? "Energy transfers to the material at the output." :
                    "Added energy can change how matter particles behave.";
                break;
            default:
                title = "Liquid Particles";
                label = stage == 0 ? "Particles are close together." :
                    stage == 1 ? "They slide and flow around each other." :
                    "Liquids take the shape of their container.";
                break;
        }

        view.Title.text = title;
        view.StageLabel.text = label;
    }

    private void UpdateBonds(IReadOnlyList<BondView> bonds, float alpha)
    {
        for (int i = 0; i < bonds.Count; i++)
        {
            BondView bond = bonds[i];
            Vector2 direction = bond.End.Position - bond.Start.Position;
            bond.Rect.anchoredPosition = (bond.Start.Position + bond.End.Position) * 0.5f;
            bond.Rect.sizeDelta = new Vector2(direction.magnitude, 4f);
            bond.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            Color color = bond.Image.color;
            color.a = alpha * 0.68f;
            bond.Image.color = color;
        }
    }

    private void SetFlowLineAlpha(IReadOnlyList<RectTransform> lines, float alpha)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            Image image = lines[i].GetComponent<Image>();
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    private void SetIceCubeAlpha(IceCubeView iceCube, float amount)
    {
        if (iceCube == null)
            return;

        float eased = Mathf.SmoothStep(0f, 1f, amount);
        Color fillColor = iceCube.Fill.color;
        fillColor.a = 0.2f * eased;
        iceCube.Fill.color = fillColor;

        for (int i = 0; i < iceCube.Edges.Count; i++)
        {
            Color edgeColor = iceCube.Edges[i].color;
            edgeColor.a = 0.82f * eased;
            iceCube.Edges[i].color = edgeColor;
        }
    }

    private void SetContainerAlpha(ContainerView container, float amount)
    {
        if (container == null)
            return;

        float eased = Mathf.SmoothStep(0f, 1f, amount);
        for (int i = 0; i < container.Edges.Count; i++)
        {
            Color edgeColor = container.Edges[i].color;
            edgeColor.a = 0.84f * eased;
            container.Edges[i].color = edgeColor;
        }
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
        float minY = -particleAreaSize.y * 0.4f;
        float maxY = particleAreaSize.y * 0.4f;

        if (position.y < minY || position.y > maxY)
        {
            velocity.y *= -1f;
            position.y = Mathf.Clamp(position.y, minY, maxY);
        }

        float maxX = GetContainerHalfWidth(position.y);
        if (position.x < -maxX || position.x > maxX)
        {
            velocity.x *= -1f;
            position.x = Mathf.Clamp(position.x, -maxX, maxX);
        }

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

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration, CutsceneContext context)
    {
        yield return AnimateFor(duration, context, (progress, elapsed, deltaTime) =>
        {
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
        });

        group.alpha = to;
    }

    private IEnumerator AnimateFor(float duration, CutsceneContext context, System.Action<float, float, float> tick)
    {
        if (duration <= 0f)
        {
            tick(1f, 0f, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float deltaTime = context.DeltaTime;
            elapsed += deltaTime;
            tick(Mathf.Clamp01(elapsed / duration), elapsed, deltaTime);
            yield return null;
        }

        tick(1f, duration, 0f);
    }

    private Color GetParticleColor(int index)
    {
        switch (cutsceneKind)
        {
            case MatterCutsceneKind.ChocolateMelting:
                return Color.Lerp(new Color(0.34f, 0.14f, 0.045f, 1f), new Color(0.86f, 0.42f, 0.14f, 1f), (index % 5) * 0.16f);
            case MatterCutsceneKind.CircuitEnergy:
                return Color.Lerp(new Color(0.25f, 0.78f, 1f, 1f), new Color(1f, 0.95f, 0.2f, 1f), index / Mathf.Max(1f, particleCount - 1f));
            case MatterCutsceneKind.LiquidFlow:
            case MatterCutsceneKind.LiquidFreezing:
                return GetJuiceParticleColor(index);
            case MatterCutsceneKind.PipeFreezing:
                return Color.Lerp(new Color(0.35f, 0.85f, 1f, 1f), new Color(0.8f, 1f, 1f, 1f), (index % 4) * 0.2f);
            default:
                return Color.Lerp(new Color(0.1f, 0.58f, 1f, 1f), new Color(0.55f, 0.9f, 1f, 1f), (index % 5) * 0.16f);
        }
    }

    private Color GetJuiceParticleColor(int index)
    {
        return Color.Lerp(new Color(0.91f, 0.42f, 0.03f, 1f), new Color(1f, 0.67f, 0.28f, 1f), (index % 5) * 0.18f);
    }

    private Color GetBackdropColor()
    {
        return cutsceneKind == MatterCutsceneKind.ChocolateMelting
            ? new Color(0.04f, 0.025f, 0.018f, 0.78f)
            : new Color(0.02f, 0.04f, 0.055f, 0.78f);
    }

    private Color GetPanelColor()
    {
        return cutsceneKind == MatterCutsceneKind.ChocolateMelting
            ? new Color(0.17f, 0.08f, 0.035f, 0.93f)
            : new Color(0.055f, 0.12f, 0.15f, 0.93f);
    }

    private Color GetTitleColor()
    {
        return cutsceneKind == MatterCutsceneKind.ChocolateMelting
            ? new Color(1f, 0.86f, 0.63f, 1f)
            : new Color(0.88f, 0.97f, 1f, 1f);
    }

    private Color GetLabelColor()
    {
        return cutsceneKind == MatterCutsceneKind.ChocolateMelting
            ? new Color(1f, 0.9f, 0.75f, 1f)
            : new Color(0.9f, 0.97f, 1f, 1f);
    }

    private RectTransform CreateRect(string objectName, RectTransform parent, Vector2 size, Vector2 position)
    {
        var rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private TextMeshProUGUI CreateLabel(string objectName, RectTransform parent, Vector2 size, Vector2 position, float fontSize)
    {
        RectTransform rect = CreateRect(objectName, parent, size, position);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.fontSize = fontSize;
        label.raycastTarget = false;
        return label;
    }

    private static Vector2 RandomDirection(System.Random random)
    {
        float angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private static void EnsureCircleSprite()
    {
        if (circleSprite != null)
            return;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1f) * 0.5f, (size - 1f) * 0.5f);
        float radius = size * 0.46f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private sealed class CutsceneView
    {
        public CutsceneView(RectTransform root, CanvasGroup group, TextMeshProUGUI title, TextMeshProUGUI stageLabel, RectTransform particleArea, List<ParticleView> particles, List<BondView> bonds, List<RectTransform> flowLines, IceCubeView iceCube, ContainerView container)
        {
            Root = root;
            Group = group;
            Title = title;
            StageLabel = stageLabel;
            ParticleArea = particleArea;
            Particles = particles;
            Bonds = bonds;
            FlowLines = flowLines;
            IceCube = iceCube;
            Container = container;
        }

        public RectTransform Root { get; }
        public CanvasGroup Group { get; }
        public TextMeshProUGUI Title { get; }
        public TextMeshProUGUI StageLabel { get; }
        public RectTransform ParticleArea { get; }
        public List<ParticleView> Particles { get; }
        public List<BondView> Bonds { get; }
        public List<RectTransform> FlowLines { get; }
        public IceCubeView IceCube { get; }
        public ContainerView Container { get; }
        public float ElapsedTime;
    }

    private sealed class IceCubeView
    {
        public IceCubeView(Image fill, List<Image> edges)
        {
            Fill = fill;
            Edges = edges;
        }

        public Image Fill { get; }
        public List<Image> Edges { get; }
    }

    private sealed class ContainerView
    {
        public ContainerView(List<Image> edges)
        {
            Edges = edges;
        }

        public List<Image> Edges { get; }
    }

    private sealed class ParticleView
    {
        public ParticleView(RectTransform rect, Image image, Vector2 solidPosition, Vector2 liquidPosition, Vector2 velocity, float phase)
        {
            Rect = rect;
            Image = image;
            SolidPosition = solidPosition;
            LiquidPosition = liquidPosition;
            Velocity = velocity;
            Phase = phase;
            Position = liquidPosition;
            StageStartPosition = liquidPosition;
        }

        public RectTransform Rect { get; }
        public Image Image { get; }
        public Vector2 SolidPosition { get; }
        public Vector2 LiquidPosition { get; }
        public Vector2 Velocity;
        public float Phase { get; }
        public Vector2 Position;
        public Vector2 StageStartPosition;
    }

    private sealed class BondView
    {
        public BondView(RectTransform rect, Image image, ParticleView start, ParticleView end)
        {
            Rect = rect;
            Image = image;
            Start = start;
            End = end;
        }

        public RectTransform Rect { get; }
        public Image Image { get; }
        public ParticleView Start { get; }
        public ParticleView End { get; }
    }
}
