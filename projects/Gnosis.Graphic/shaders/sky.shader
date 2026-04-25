using gg_shader::f32::{vec2, vec3, vec4, mat4}

shader sky {
    uniform mvp: mat4,
    uniform model: mat4,

    cbuffer atmosphere {
        sun_direction: vec3,
        rayleigh_scattering: vec3,
        mie_scattering: vec3,
        mie_directional_g: f32,
        sun_intensity: f32,
    },

    varying v_uv: vec2,
    varying v_ray_direction: vec3,

    [vertex_main]
    vs_main(position: vec3, normal: vec3, uv: vec2) -> vec4 {
        let clip_pos = mvp * vec4(position, 1.0)
        v_uv = uv
        v_ray_direction = normalize((model * vec4(position, 1.0)).xyz)
        let mut output_pos = clip_pos
        output_pos.z = clip_pos.w
        return output_pos
    }

    [fragment_main]
    fs_main() -> vec4 {
        let ray_dir = normalize(v_ray_direction)
        let sun_dir = normalize(atmosphere.sun_direction)

        let sky_color = compute_atmospheric_scattering(ray_dir, sun_dir)
        return vec4(sky_color, 1.0)
    }

    micro compute_atmospheric_scattering(ray_dir: vec3, sun_dir: vec3) -> vec3 {
        let cos_theta = dot(ray_dir, sun_dir)

        let rayleigh_coeff = atmosphere.rayleigh_scattering
        let mie_coeff = atmosphere.mie_scattering
        let g = atmosphere.mie_directional_g

        let rayleigh_phase = 3.0 / (16.0 * 3.14159265) * (1.0 + cos_theta * cos_theta)

        let g2 = g * g
        let mie_phase_numerator = (1.0 - g2)
        let mie_phase_denominator = pow(1.0 + g2 - 2.0 * g * cos_theta, 1.5) * 4.0 * 3.14159265
        let mie_phase = mie_phase_numerator / mie_phase_denominator

        let zenith_angle = max(ray_dir.y, 0.0)
        let rayleigh_factor = pow(1.0 - zenith_angle, 2.0)

        let rayleigh = rayleigh_coeff * rayleigh_factor * rayleigh_phase
        let mie = mie_coeff * mie_phase

        let sun_falloff = pow(max(cos_theta, 0.0), 512.0)
        let sun_disc = atmosphere.sun_intensity * sun_falloff * 0.02

        let horizon_factor = pow(1.0 - abs(ray_dir.y), 8.0)
        let horizon_color = vec3(0.8, 0.5, 0.3) * horizon_factor * 0.3

        let result = (rayleigh + mie) * atmosphere.sun_intensity + horizon_color + sun_disc
        return result
    }
}
