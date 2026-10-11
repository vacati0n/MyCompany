// The closed element set (the graphics change, decision D-001 of its design). Every element is drawn from its specification
// fields and the company style alone; text is set as a text node, never as markup. Geometry is in frame units of the
// configured profile; an angle is in degrees clockwise from straight up. Nothing here holds a subject term, a position, a
// label or a number to show: a missing field fails the frame, naming it.

import React from 'react';
import {need} from './motion.js';

const direction = (angle) => {
	const r = (angle * Math.PI) / 180;
	return [Math.sin(r), -Math.cos(r)];
};

const toward = (x, y, angle, length) => {
	const [dx, dy] = direction(angle);
	return [x + dx * length, y + dy * length];
};

const lengthOf = (points) => {
	let total = 0;
	for (let i = 1; i < points.length; i++) {
		total += Math.hypot(points[i][0] - points[i - 1][0], points[i][1] - points[i - 1][1]);
	}

	return total;
};

// The first fraction of a polyline, by length.
export const partOf = (points, fraction) => {
	if (fraction >= 1 || points.length < 2) {
		return points;
	}

	const wanted = lengthOf(points) * Math.max(0, fraction);
	const part = [points[0]];
	let walked = 0;
	for (let i = 1; i < points.length; i++) {
		const step = Math.hypot(points[i][0] - points[i - 1][0], points[i][1] - points[i - 1][1]);
		if (walked + step >= wanted) {
			const t = step === 0 ? 0 : (wanted - walked) / step;
			part.push([points[i - 1][0] + (points[i][0] - points[i - 1][0]) * t, points[i - 1][1] + (points[i][1] - points[i - 1][1]) * t]);
			return part;
		}

		walked += step;
		part.push(points[i]);
	}

	return part;
};

// A path's points as a polyline: straight, or a smooth curve through its points (closed for a loop), sampled evenly.
export const sampled = (element) => {
	const points = need(element, 'points');
	const form = need(element, 'form');
	if (form === 'polyline') {
		return points;
	}

	const closed = form === 'loop';
	const at = (i) => (closed ? points[(i + points.length) % points.length] : points[Math.max(0, Math.min(points.length - 1, i))]);
	const segments = closed ? points.length : points.length - 1;
	const out = [];
	for (let s = 0; s < segments; s++) {
		const [p0, p1, p2, p3] = [at(s - 1), at(s), at(s + 1), at(s + 2)];
		for (let k = 0; k < 24; k++) {
			const t = k / 24;
			const t2 = t * t;
			const t3 = t2 * t;
			const c = (a, b, d, e) => 0.5 * (2 * b + (-a + d) * t + (2 * a - 5 * b + 4 * d - e) * t2 + (-a + 3 * b - 3 * d + e) * t3);
			out.push([c(p0[0], p1[0], p2[0], p3[0]), c(p0[1], p1[1], p2[1], p3[1])]);
		}
	}

	out.push(closed ? points[0] : points[points.length - 1]);
	return out;
};

const line = (points) => points.map((p) => `${p[0].toFixed(2)},${p[1].toFixed(2)}`).join(' ');

const Arrow = ({points, size, colour}) => {
	if (points.length < 2) {
		return null;
	}

	const [x1, y1] = points[points.length - 2];
	const [x2, y2] = points[points.length - 1];
	const length = Math.hypot(x2 - x1, y2 - y1);
	if (length === 0) {
		return null;
	}

	const ux = (x2 - x1) / length;
	const uy = (y2 - y1) / length;
	const head = [
		[x2, y2],
		[x2 - ux * size - uy * size * 0.45, y2 - uy * size + ux * size * 0.45],
		[x2 - ux * size + uy * size * 0.45, y2 - uy * size - ux * size * 0.45],
	];
	return <polygon points={line(head)} fill={colour} />;
};

const Stroke = ({points, colour, width, dashed, arrow, style, opacity}) => (
	<g opacity={opacity}>
		<polyline
			points={line(points)}
			fill="none"
			stroke={colour}
			strokeWidth={width}
			strokeLinecap="round"
			strokeLinejoin="round"
			strokeDasharray={dashed ? need(style, 'dash').join(' ') : undefined}
		/>
		{arrow ? <Arrow points={points} size={need(style, 'arrow')} colour={colour} /> : null}
	</g>
);

const wedge = (x, y, radius, from, to) => {
	const [sx, sy] = toward(x, y, from, radius);
	const [ex, ey] = toward(x, y, to, radius);
	const large = Math.abs(to - from) > 180 ? 1 : 0;
	return `M ${x} ${y} L ${sx.toFixed(2)} ${sy.toFixed(2)} A ${radius} ${radius} 0 ${large} 1 ${ex.toFixed(2)} ${ey.toFixed(2)} Z`;
};

export const Element = ({element, state, value, style, family, find}) => {
	const colours = need(style, 'colours');
	const strokes = need(style, 'strokes');
	const focus = element.focus === true;
	const ink = focus ? need(colours, 'accent') : need(colours, 'line');
	const width = focus || element.weight === 'heavy' ? need(strokes, 'heavy') : need(strokes, 'line');
	const opacity = state.opacity;
	const v = (key) => (state.values[key] !== undefined ? state.values[key] : value(need(element, key)));

	switch (need(element, 'type')) {
		case 'text': {
			const size = need(need(style, 'sizes'), need(element, 'size'));
			return (
				<text
					x={value(need(element, 'x'))}
					y={value(need(element, 'y'))}
					textAnchor={need(element, 'anchor')}
					fontFamily={family}
					fontStyle={element.face === 'italic' ? 'italic' : 'normal'}
					fontWeight={400}
					fontSize={size}
					fill={focus ? need(colours, 'accent') : need(colours, 'text')}
					opacity={opacity * state.drawn}
				>
					{need(element, 'text')}
				</text>
			);
		}

		case 'point': {
			let [x, y] = [v('x'), v('y')];
			if (state.values.along) {
				const path = find(state.values.along.path);
				const part = partOf(sampled(path), state.values.along.at);
				[x, y] = part[part.length - 1];
			}

			const filled = need(element, 'filled');
			return (
				<circle
					cx={x}
					cy={y}
					r={v('r')}
					fill={filled ? ink : need(colours, 'background')}
					stroke={ink}
					strokeWidth={need(strokes, 'line')}
					opacity={opacity * state.drawn}
				/>
			);
		}

		case 'bearing': {
			const [x, y] = [v('x'), v('y')];
			const end = toward(x, y, v('angle'), v('length') * state.drawn);
			return <Stroke points={[[x, y], end]} colour={ink} width={width} dashed={need(element, 'dashed')} arrow={need(element, 'arrow') && state.drawn > 0.98} style={style} opacity={opacity} />;
		}

		case 'path':
			return (
				<Stroke
					points={partOf(sampled(element), state.drawn)}
					colour={ink}
					width={width}
					dashed={need(element, 'dashed')}
					arrow={need(element, 'arrow') && state.drawn > 0.98}
					style={style}
					opacity={opacity}
				/>
			);

		case 'axis': {
			const points = partOf([[v('x1'), v('y1')], [v('x2'), v('y2')]], state.drawn);
			return <Stroke points={points} colour={focus ? ink : need(colours, 'secondary')} width={need(strokes, 'thin')} dashed={false} arrow={state.drawn > 0.98} style={style} opacity={opacity} />;
		}

		case 'trace': {
			const [x, y, w, h] = [v('x'), v('y'), v('width'), v('height')];
			const form = need(element, 'form');
			if (form === 'oscillation') {
				const cycles = v('cycles');
				const points = [];
				for (let i = 0; i <= 600; i++) {
					const u = i / 600;
					points.push([x + w * u, y + h / 2 - (h / 2) * Math.sin(2 * Math.PI * cycles * u)]);
				}

				return <Stroke points={partOf(points, state.drawn)} colour={ink} width={width} dashed={false} arrow={false} style={style} opacity={opacity} />;
			}

			const centre = partOf([[x, y + h], [x + w, y]], state.drawn);
			if (form === 'linear') {
				return <Stroke points={centre} colour={ink} width={width} dashed={false} arrow={false} style={style} opacity={opacity} />;
			}

			const spread = v('spread') * h * state.drawn;
			const [ex, ey] = centre[centre.length - 1];
			return (
				<g opacity={opacity}>
					<polygon points={line([[x, y + h], [ex, ey - spread], [ex, ey + spread]])} fill={need(colours, 'shade')} />
					<Stroke points={centre} colour={ink} width={width} dashed={false} arrow={false} style={style} opacity={1} />
				</g>
			);
		}

		case 'angle': {
			const [x, y, radius] = [v('x'), v('y'), v('radius')];
			const from = v('from');
			const to = state.values.angle !== undefined ? state.values.angle : value(need(element, 'to'));
			const end = from + (to - from) * state.drawn;
			const points = [];
			for (let i = 0; i <= 90; i++) {
				points.push(toward(x, y, from + ((end - from) * i) / 90, radius));
			}

			return <Stroke points={points} colour={ink} width={need(strokes, 'line')} dashed={false} arrow={false} style={style} opacity={opacity} />;
		}

		case 'region': {
			const [x, y, a, b, angle] = [v('x'), v('y'), v('a'), v('b'), v('angle')];
			const shade = need(element, 'shading') === 'shade';
			const fill = shade ? need(colours, 'shade') : 'none';
			const stroke = shade && !focus ? 'none' : ink;
			const common = {fill, stroke, strokeWidth: need(strokes, 'line'), opacity: opacity * state.drawn};
			switch (need(element, 'form')) {
				case 'sector':
					return <path d={wedge(x, y, a, angle - b, angle + b)} {...common} />;
				case 'cone': {
					const left = toward(x, y, angle - b, a);
					const right = toward(x, y, angle + b, a);
					return <polygon points={line([[x, y], left, right])} {...common} />;
				}

				case 'spread': {
					const layers = [];
					for (let k = 8; k >= 1; k--) {
						layers.push(<path key={k} d={wedge(x, y, a, angle - (b * k) / 8, angle + (b * k) / 8)} fill={need(colours, focus ? 'accent' : 'secondary')} fillOpacity={0.09} stroke="none" />);
					}

					return <g opacity={opacity * state.drawn}>{layers}</g>;
				}

				case 'ellipse':
					return <ellipse cx={x} cy={y} rx={a} ry={b} transform={`rotate(${angle} ${x} ${y})`} {...common} />;
				default:
					return <rect x={x} y={y} width={a} height={b} {...common} />;
			}
		}

		case 'panel':
			return (
				<rect x={v('x')} y={v('y')} width={v('w')} height={v('h')} fill="none" stroke={need(colours, 'secondary')} strokeWidth={need(strokes, 'thin')} opacity={opacity * state.drawn} />
			);

		case 'blocks': {
			const [x, y, bw, bh, gap, count] = [v('x'), v('y'), v('boxWidth'), v('boxHeight'), v('gap'), v('count')];
			const shown = count * state.drawn;
			const parts = [];
			for (let i = 0; i < count; i++) {
				const seen = Math.max(0, Math.min(1, shown - i));
				const left = x + i * (bw + gap);
				parts.push(<rect key={`b${i}`} x={left} y={y} width={bw} height={bh} rx={8} fill="none" stroke={ink} strokeWidth={need(strokes, 'line')} opacity={seen} />);
				if (i < count - 1) {
					const arrow = [[left + bw + 8, y + bh / 2], [left + bw + gap - 8, y + bh / 2]];
					parts.push(<Stroke key={`a${i}`} points={arrow} colour={ink} width={need(strokes, 'line')} dashed={false} arrow style={style} opacity={Math.max(0, Math.min(1, shown - i - 1))} />);
				}
			}

			return <g opacity={opacity}>{parts}</g>;
		}

		case 'bars': {
			const [x, y, w, h] = [v('x'), v('y'), v('width'), v('height')];
			const extents = need(element, 'extents');
			const pitch = h / extents.length;
			return (
				<g opacity={opacity}>
					{extents.map((extent, i) => (
						<rect key={i} x={x} y={y + i * pitch + pitch * 0.2} width={w * value(extent) * state.drawn} height={pitch * 0.6} fill={need(colours, 'shade')} stroke={ink} strokeWidth={need(strokes, 'line')} />
					))}
				</g>
			);
		}

		default:
			throw new Error(element.type);
	}
};
