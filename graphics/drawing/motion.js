// Motion over a graphic (the graphics change, decision D-005 of its design). The frame index is the only time: no clock, no
// random source. A still shows its final state. A motion graphic of N frames moves over its motion window, the first
// floor(N times the style's motion-end fraction) frames, and holds its final state after it. Every motion of the closed set
// acts over its own part of the window, eased; nothing here holds a quantity, a label or a subject.

const clamp = (value) => Math.max(0, Math.min(1, value));

const ease = (t) => t * t * (3 - 2 * t);

export const need = (node, key) => {
	if (node[key] === undefined || node[key] === null) {
		throw new Error(`${node.id || node.type}.${key}`);
	}

	return node[key];
};

export const progressAt = (frame, timing, style) => {
	if (need(timing, 'kind') === 'still') {
		return 1;
	}

	const window = Math.floor(need(timing, 'frames') * need(style, 'motionEnd'));
	return window <= 1 ? 1 : clamp(frame / (window - 1));
};

const local = (motion, progress) => ease(clamp((progress - need(motion, 'from')) / (need(motion, 'to') - need(motion, 'from'))));

// The state every element is drawn in at one progress: how much of it is drawn, its opacity, and any value a motion sets.
export const stateAt = (specification, progress) => {
	const states = {};
	const of = (id) => {
		states[id] = states[id] || {drawn: 1, opacity: 1, values: {}};
		return states[id];
	};

	for (const motion of specification.motion || []) {
		const t = local(motion, progress);
		switch (need(motion, 'type')) {
			case 'reveal':
				for (const id of need(motion, 'targets')) {
					of(id).drawn *= t;
				}

				break;
			case 'rotate':
				for (const id of need(motion, 'targets')) {
					of(id).values.angle = need(motion, 'fromAngle') + (need(motion, 'toAngle') - need(motion, 'fromAngle')) * t;
				}

				break;
			case 'grow':
				of(need(motion, 'target')).values[need(motion, 'property')] =
					need(motion, 'fromValue') + (need(motion, 'toValue') - need(motion, 'fromValue')) * t;
				break;
			case 'follow':
				of(need(motion, 'target')).values.along = {path: need(motion, 'path'), at: t};
				break;
			case 'sequence': {
				const sets = need(motion, 'sets');
				const span = (need(motion, 'to') - need(motion, 'from')) / sets.length;
				const fadeIn = (i) => ease(clamp((progress - (need(motion, 'from') + i * span)) / (span * 0.3)));
				sets.forEach((set, i) => {
					const shown = need(motion, 'accumulate') || i === sets.length - 1 ? fadeIn(i) : fadeIn(i) * (1 - fadeIn(i + 1));
					for (const id of set) {
						of(id).opacity *= shown;
					}
				});
				break;
			}

			default:
				throw new Error(motion.type);
		}
	}

	return (id) => states[id] || {drawn: 1, opacity: 1, values: {}};
};
