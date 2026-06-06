using UnityEngine;
using UnityEngine.Rendering;

public static class EnemyMaterialFixer
{
    public static int FixObjectMaterials(GameObject root, bool useSharedMaterials)
    {
        if (root == null)
        {
            return 0;
        }

        Shader fallbackShader = ResolveFallbackShader();
        if (fallbackShader == null)
        {
            return 0;
        }

        int fixedCount = 0;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] mats = useSharedMaterials ? renderer.sharedMaterials : renderer.materials;
            if (mats == null || mats.Length == 0)
            {
                continue;
            }

            bool changed = false;
            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat == null)
                {
                    mats[m] = new Material(fallbackShader);
                    changed = true;
                    fixedCount++;
                    continue;
                }

                Shader shader = mat.shader;
                if (shader == null || !shader.isSupported)
                {
                    mat.shader = fallbackShader;
                    changed = true;
                    fixedCount++;
                }
            }

            if (changed)
            {
                if (useSharedMaterials)
                {
                    renderer.sharedMaterials = mats;
                }
                else
                {
                    renderer.materials = mats;
                }
            }
        }

        return fixedCount;
    }

    public static Shader ResolveFallbackShader()
    {
        RenderPipelineAsset rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null)
        {
            string rpName = rp.GetType().Name;
            if (rpName.Contains("HD"))
            {
                Shader hdrp = Shader.Find("HDRP/Lit");
                if (hdrp != null)
                {
                    return hdrp;
                }
            }

            if (rpName.Contains("Universal"))
            {
                Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                if (urp != null)
                {
                    return urp;
                }
            }
        }

        Shader standard = Shader.Find("Standard");
        if (standard != null)
        {
            return standard;
        }

        Shader lit = Shader.Find("Lit");
        if (lit != null)
        {
            return lit;
        }

        Shader legacy = Shader.Find("Legacy Shaders/Diffuse");
        if (legacy != null)
        {
            return legacy;
        }

        return Shader.Find("Unlit/Color");
    }
}
