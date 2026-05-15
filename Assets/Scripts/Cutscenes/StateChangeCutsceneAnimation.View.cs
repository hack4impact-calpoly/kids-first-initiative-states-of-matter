using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class StateChangeCutsceneAnimation
{
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
        IStateChangeCutsceneBehavior behavior = CurrentBehavior;
        ContainerView container = behavior != null && behavior.UsesPlasmaTubeContainer
            ? CreatePlasmaTube(particleArea)
            : CreateContainer(particleArea);

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

    private ContainerView CreatePlasmaTube(RectTransform parent)
    {
        float tubeWidth = particleAreaSize.x * 0.82f;
        float tubeHeight = particleAreaSize.y * 0.46f;
        float thickness = 9f;
        float electrodeThickness = 13f;
        float halfWidth = tubeWidth * 0.5f;
        float halfHeight = tubeHeight * 0.5f;
        float electrodeInset = tubeWidth * 0.18f;

        var edges = new List<Image>(9)
        {
            CreateContainerEdge(parent, "Plasma Tube Top", new Vector2(tubeWidth, thickness), new Vector2(0f, halfHeight), 0f),
            CreateContainerEdge(parent, "Plasma Tube Bottom", new Vector2(tubeWidth, thickness), new Vector2(0f, -halfHeight), 0f),
            CreateContainerEdge(parent, "Plasma Tube Left Cap", new Vector2(thickness, tubeHeight), new Vector2(-halfWidth, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Right Cap", new Vector2(thickness, tubeHeight), new Vector2(halfWidth, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Left Electrode", new Vector2(electrodeThickness, tubeHeight * 0.58f), new Vector2(-halfWidth + electrodeInset, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Right Electrode", new Vector2(electrodeThickness, tubeHeight * 0.58f), new Vector2(halfWidth - electrodeInset, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Left Lead", new Vector2(electrodeInset, thickness), new Vector2(-halfWidth + electrodeInset * 0.5f, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Right Lead", new Vector2(electrodeInset, thickness), new Vector2(halfWidth - electrodeInset * 0.5f, 0f), 0f),
            CreateContainerEdge(parent, "Plasma Tube Center Glow", new Vector2(tubeWidth * 0.62f, thickness * 1.6f), Vector2.zero, 0f)
        };

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

    private Color GetParticleColor(int index)
    {
        IStateChangeCutsceneBehavior behavior = CurrentBehavior;
        if (behavior != null)
            return behavior.GetParticleColor(index);

        switch (cutsceneKind)
        {
            default:
                return GetWaterParticleColor(index);
        }
    }

    private Color GetWaterParticleColor(int index)
    {
        return Color.Lerp(new Color(0.1f, 0.58f, 1f, 1f), new Color(0.55f, 0.9f, 1f, 1f), (index % 5) * 0.16f);
    }

    private Color GetJuiceParticleColor(int index)
    {
        return Color.Lerp(new Color(0.91f, 0.42f, 0.03f, 1f), new Color(1f, 0.67f, 0.28f, 1f), (index % 5) * 0.18f);
    }

    private Color GetWaxParticleColor(int index)
    {
        return Color.Lerp(new Color(1f, 0.86f, 0.56f, 1f), new Color(1f, 0.63f, 0.26f, 1f), (index % 5) * 0.16f);
    }

    private Color GetMeltedWaxParticleColor(int index)
    {
        return Color.Lerp(new Color(1f, 0.48f, 0.14f, 1f), new Color(1f, 0.86f, 0.22f, 1f), (index % 4) * 0.22f);
    }

    private Color GetGasParticleColor(int index)
    {
        return Color.Lerp(new Color(0.43f, 0.82f, 1f, 0.9f), new Color(0.7f, 0.9f, 1f, 0.9f), (index % 5) * 0.16f);
    }

    private Color GetPlasmaParticleColor(int index, float elapsed)
    {
        float pulse = (Mathf.Sin(elapsed * 7f + index * 0.8f) + 1f) * 0.5f;
        return Color.Lerp(new Color(0.18f, 0.96f, 1f, 1f), new Color(1f, 0.24f, 0.94f, 1f), pulse);
    }

    private Color GetBackdropColor()
    {
        if (cutsceneKind == MatterCutsceneKind.ChocolateMelting || cutsceneKind == MatterCutsceneKind.CircuitCandleMelting)
            return new Color(0.04f, 0.025f, 0.018f, 0.78f);

        if (cutsceneKind == MatterCutsceneKind.CircuitPlasmaIonizing)
            return new Color(0.018f, 0.012f, 0.052f, 0.8f);

        return new Color(0.02f, 0.04f, 0.055f, 0.78f);
    }

    private Color GetPanelColor()
    {
        if (cutsceneKind == MatterCutsceneKind.ChocolateMelting || cutsceneKind == MatterCutsceneKind.CircuitCandleMelting)
            return new Color(0.17f, 0.08f, 0.035f, 0.93f);

        if (cutsceneKind == MatterCutsceneKind.CircuitPlasmaIonizing)
            return new Color(0.04f, 0.035f, 0.16f, 0.93f);

        return new Color(0.055f, 0.12f, 0.15f, 0.93f);
    }

    private Color GetTitleColor()
    {
        if (cutsceneKind == MatterCutsceneKind.ChocolateMelting || cutsceneKind == MatterCutsceneKind.CircuitCandleMelting)
            return new Color(1f, 0.86f, 0.63f, 1f);

        if (cutsceneKind == MatterCutsceneKind.CircuitPlasmaIonizing)
            return new Color(0.9f, 0.86f, 1f, 1f);

        return new Color(0.88f, 0.97f, 1f, 1f);
    }

    private Color GetLabelColor()
    {
        if (cutsceneKind == MatterCutsceneKind.ChocolateMelting || cutsceneKind == MatterCutsceneKind.CircuitCandleMelting)
            return new Color(1f, 0.9f, 0.75f, 1f);

        if (cutsceneKind == MatterCutsceneKind.CircuitPlasmaIonizing)
            return new Color(0.92f, 0.9f, 1f, 1f);

        return new Color(0.9f, 0.97f, 1f, 1f);
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
