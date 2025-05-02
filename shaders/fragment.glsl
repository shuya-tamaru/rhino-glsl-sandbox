#version 330 core

out vec4 FragColor;
uniform float uTime;
uniform vec2 uResolution;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));

    vec2 u = f * f * (3.0 - 2.0 * f);

    return mix(mix(a, b, u.x), mix(c, d, u.x), u.y);
}

void main()
{
    vec2 uv = gl_FragCoord.xy / uResolution * 5.0;

    float speed = uTime * 2.0;
    float r = noise(uv + vec2(speed, 0.0));
    float g = noise(uv + vec2(0.0, speed));
    float b = noise(uv + vec2(speed, speed));

    r = sin(r * 6.2831 + uTime) * 0.5 + 0.5;
    g = sin(g * 6.2831 + uTime + 1.0) * 0.5 + 0.5;
    b = sin(b * 6.2831 + uTime + 2.0) * 0.5 + 0.5;

    FragColor = vec4(r, g, b, 0.5);
}
