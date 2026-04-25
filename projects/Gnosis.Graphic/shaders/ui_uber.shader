using gg_shader::f32::{vec2, vec3, vec4}
using gg_shader::texture2D
using gg_shader::sampler

shader ui_uber {
    uniform sdf_spread: f32 = 0.5,
    uniform outline_width: f32 = 0.0,
    uniform outline_color: vec4 = vec4(0.0, 0.0, 0.0, 0.0),
    uniform shadow_offset: vec2 = vec2(0.0, 0.0),
    uniform shadow_width: f32 = 0.0,
    uniform shadow_color: vec4 = vec4(0.0, 0.0, 0.0, 0.0),
    uniform style_id: i32 = 0,
    uniform smoothness: f32 = 0.02,
    uniform screen_size: vec2 = vec2(1.0, 1.0),

    texture sdf_atlas: texture2D,
    sampler atlas_sampler,

    varying v_color: vec4,
    varying v_uv: vec2,
    varying v_rect_size: vec2,
    varying v_corner_radius: vec4,
    varying v_draw_type: f32,
    varying v_border_width: f32,
    varying v_border_color: vec4,

    [vertex_main]
    vs_main(position: vec3, color: vec4, uv: vec2, rect_size: vec2, corner_radius: vec4, draw_type: f32, border_width: f32, border_color: vec4) -> vec4 {
        v_color = color
        v_uv = uv
        v_rect_size = rect_size
        v_corner_radius = corner_radius
        v_draw_type = draw_type
        v_border_width = border_width
        v_border_color = border_color
        return vec4(position, 1.0)
    }

    [fragment_main]
    fs_main() -> vec4 {
        if v_draw_type < 0.5 {
            return draw_sdf_text()
        }
        if v_draw_type < 1.5 {
            return draw_rounded_rect()
        }
        if v_draw_type < 2.5 {
            return draw_rounded_rect_border()
        }
        if v_draw_type < 3.5 {
            return draw_line()
        }
        return draw_sdf_text()
    }

    micro draw_sdf_text() -> vec4 {
        let dist = sample_sdf(v_uv)
        let edge = 0.5
        let smooth_edge = max(smoothness, 0.001)
        let fill_alpha = smoothstep(edge - smooth_edge, edge + smooth_edge, dist)
        let outline_alpha = compute_outline(dist, edge, smooth_edge, outline_width, sdf_spread)
        let shadow_alpha = compute_shadow(v_uv, edge, smooth_edge, shadow_width, sdf_spread, shadow_offset)

        let out_r = shadow_color.r * shadow_alpha * shadow_color.a
                  + outline_color.r * outline_alpha
                  + v_color.r * fill_alpha * v_color.a
        let out_g = shadow_color.g * shadow_alpha * shadow_color.a
                  + outline_color.g * outline_alpha
                  + v_color.g * fill_alpha * v_color.a
        let out_b = shadow_color.b * shadow_alpha * shadow_color.a
                  + outline_color.b * outline_alpha
                  + v_color.b * fill_alpha * v_color.a
        let out_a = min(shadow_alpha * shadow_color.a + outline_alpha + fill_alpha * v_color.a, 1.0)

        return vec4(out_r, out_g, out_b, out_a)
    }

    micro draw_rounded_rect() -> vec4 {
        let half_size = v_rect_size * 0.5
        let local_pos = v_uv * v_rect_size - half_size
        let dist = sdf_rounded_rect(local_pos, half_size, v_corner_radius)
        let smooth_edge = max(smoothness, 0.001)
        let alpha = smoothstep(-smooth_edge, smooth_edge, dist)
        return vec4(v_color.r, v_color.g, v_color.b, v_color.a * alpha)
    }

    micro draw_rounded_rect_border() -> vec4 {
        let half_size = v_rect_size * 0.5
        let local_pos = v_uv * v_rect_size - half_size
        let outer_dist = sdf_rounded_rect(local_pos, half_size, v_corner_radius)
        let inner_half = half_size - vec2(v_border_width, v_border_width)
        let inner_radius = max(v_corner_radius - vec4(v_border_width, v_border_width, v_border_width, v_border_width), vec4(0.0, 0.0, 0.0, 0.0))
        let inner_dist = sdf_rounded_rect(local_pos, inner_half, inner_radius)
        let smooth_edge = max(smoothness, 0.001)
        let outer_alpha = smoothstep(-smooth_edge, smooth_edge, outer_dist)
        let inner_alpha = smoothstep(-smooth_edge, smooth_edge, inner_dist)
        let border_alpha = outer_alpha * (1.0 - inner_alpha)
        let fill_alpha = outer_alpha * inner_alpha
        let r = v_border_color.r * border_alpha + v_color.r * fill_alpha * v_color.a
        let g = v_border_color.g * border_alpha + v_color.g * fill_alpha * v_color.a
        let b = v_border_color.b * border_alpha + v_color.b * fill_alpha * v_color.a
        let a = border_alpha + fill_alpha * v_color.a
        return vec4(r, g, b, a)
    }

    micro draw_line() -> vec4 {
        return vec4(v_color.r, v_color.g, v_color.b, v_color.a)
    }

    micro sample_sdf(uv: vec2) -> f32 {
        let sampled = texture(sdf_atlas, atlas_sampler, uv)
        return sampled.r
    }

    micro compute_outline(dist: f32, edge: f32, smooth_edge: f32, width: f32, spread: f32) -> f32 {
        if width <= 0.0 {
            return 0.0
        }

        let outer_edge = edge + width / spread
        let inner = smoothstep(edge - smooth_edge, edge + smooth_edge, dist)
        let outer = smoothstep(outer_edge - smooth_edge, outer_edge + smooth_edge, dist)
        return inner * (1.0 - outer)
    }

    micro compute_shadow(uv: vec2, edge: f32, smooth_edge: f32, width: f32, spread: f32, offset: vec2) -> f32 {
        if width <= 0.0 {
            return 0.0
        }

        let shadow_uv = uv - offset
        let shadow_dist = sample_sdf(shadow_uv)
        let shadow_edge = edge - width / spread
        return smoothstep(shadow_edge - smooth_edge, shadow_edge + smooth_edge, shadow_dist)
    }

    micro sdf_rounded_rect(p: vec2, half_size: vec2, radii: vec4) -> f32 {
        let r_tl = radii.x
        let r_tr = radii.y
        let r_br = radii.z
        let r_bl = radii.w

        let select_x = step(0.0, p.x)
        let select_y = step(0.0, p.y)
        let r_top = mix(r_tl, r_tr, select_x)
        let r_bottom = mix(r_bl, r_br, select_x)
        let r = mix(r_top, r_bottom, select_y)

        let corner_center = mix(
            mix(vec2(-half_size.x + r_tl, -half_size.y + r_tl), vec2(half_size.x - r_tr, -half_size.y + r_tr), select_x),
            mix(vec2(-half_size.x + r_bl, half_size.y - r_bl), vec2(half_size.x - r_br, half_size.y - r_br), select_x),
            select_y
        )

        let is_corner = step(half_size.x - abs(p.x), r) * step(half_size.y - abs(p.y), r)

        let q = abs(p) - half_size + vec2(r, r)
        let corner_dist = length(max(q, vec2(0.0, 0.0))) - r

        let box_dist = length(max(abs(p) - half_size, vec2(0.0, 0.0)))

        return mix(box_dist, corner_dist, is_corner)
    }
}
