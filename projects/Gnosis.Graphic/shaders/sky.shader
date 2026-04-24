struct SkyVertexInput {
    position: vec3<f32>,
    normal: vec3<f32>,
    uv: vec2<f32>,
}

struct SkyVertexOutput {
    position: vec4<f32>,
    uv: vec2<f32>,
    ray_direction: vec3<f32>,
}

[Vertex]
micro vs_main(input: SkyVertexInput) -> SkyVertexOutput {
    let mut output: SkyVertexOutput;
    output.position = uniforms.mvp * vec4<f32>(input.position, 1.0);
    output.position.z = output.position.w;
    output.uv = input.uv;
    output.ray_direction = normalize((uniforms.model * vec4<f32>(input.position, 1.0)).xyz);
    return output;
}

[Fragment]
micro fs_main(input: SkyVertexOutput) -> vec4<f32> {
    let ray_dir = normalize(input.ray_direction);
    let sun_dir = normalize(atmosphere.sun_direction);

    let sky_color = compute_atmospheric_scattering(ray_dir, sun_dir);
    return vec4<f32>(sky_color, 1.0);
}

micro compute_atmospheric_scattering(ray_dir: vec3<f32>, sun_dir: vec3<f32>) -> vec3<f32> {
    let cos_theta = dot(ray_dir, sun_dir);

    let rayleigh_coeff = atmosphere.rayleigh_scattering;
    let mie_coeff = atmosphere.mie_scattering;
    let g = atmosphere.mie_directional_g;

    let rayleigh_phase = 3.0 / (16.0 * 3.14159265) * (1.0 + cos_theta * cos_theta);

    let g2 = g * g;
    let mie_phase_numerator = (1.0 - g2);
    let mie_phase_denominator = pow(1.0 + g2 - 2.0 * g * cos_theta, 1.5) * 4.0 * 3.14159265;
    let mie_phase = mie_phase_numerator / mie_phase_denominator;

    let zenith_angle = max(ray_dir.y, 0.0);
    let rayleigh_factor = pow(1.0 - zenith_angle, 2.0);

    let rayleigh = rayleigh_coeff * rayleigh_factor * rayleigh_phase;
    let mie = mie_coeff * mie_phase;

    let sun_falloff = pow(max(cos_theta, 0.0), 512.0);
    let sun_disc = atmosphere.sun_intensity * sun_falloff * 0.02;

    let horizon_factor = pow(1.0 - abs(ray_dir.y), 8.0);
    let horizon_color = vec3<f32>(0.8, 0.5, 0.3) * horizon_factor * 0.3;

    let result = (rayleigh + mie) * atmosphere.sun_intensity + horizon_color + sun_disc;
    return result;
}
