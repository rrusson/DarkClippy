const baseUrl = window.location.origin + window.location.pathname.replace(/\/$/, '');
var clippyAgent; // Add a global reference to Clippy.
var isIdle = true;
var mousePosition = { x: 0, y: 0 };

$(document).mousemove(function (event) {
	mousePosition.x = event.pageX;
	mousePosition.y = event.pageY;
});

$(document).ready(function () {
	$('#question').keydown(function (event) {
		if (event.key === 'Enter') {
			event.preventDefault();
			$('#submit').trigger('click');
		}
	});

	$('#submit').click(async function () {
		const question = $('#question').val();
		const responseDiv = $('#response');
		const spinnerDiv = $('#spinner');
		const submitButton = $('#submit');

		if (!question || question == '') {
			clippyAgent.play('GetAttention');
			clippyAgent.speak('You feeling OK?');
			return;
		}

		isIdle = false;

		try {
			spinnerDiv.show();
			submitButton.prop('disabled', true);
			clippyAgent.play('Writing');

			let result = await getResponse(question);
			const santizedResult = removeHtmlTags(result);

			clippyAgent.speak(santizedResult);
			setTimeout(() => $('#question').val(''), 8000);
		} catch (error) {
			responseDiv.text('Error: ' + error);
		} finally {
			spinnerDiv.hide();
			submitButton.prop('disabled', false);
			isIdle = true;
		}
	});

	async function getResponse(input) {
		let question = input.replace('.', '?');

		if (!question.endsWith('?')) {
			question += '?';
			console.log(question);
		}

		if (question.endsWith('time is it?') || question.endsWith('the current time?') || question.endsWith('the time?')) {
			return 'It\'s ' + new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '.';
		}

		if (question.endsWith('today\'s date?') || question.endsWith('what day is it?')
			|| question.endsWith('is today?') || question.endsWith('the date?')) {
			return 'Today\'s date is ' + new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' }) + '.';
		}

		let response = await fetch(`${baseUrl}/api/chat`, {
			method: 'POST',
			headers: {
				'Content-Type': 'application/json'
			},
			body: JSON.stringify(question)
		});

		let result = await response.text();
		return result;
	}

	function removeHtmlTags(input) {
		const regex = /<[^>]+>/g;
		return input.replace(regex, '');
	}

	function getQuip() {
		var rnd = Math.round(Math.random() * 20);
		switch (rnd) {
			case 1: return 'I\'m Clippy.';
			case 2: return 'Hi, I\'m Clippy and I\'m here to help!';
			case 3: return 'Don\'t you have anything better to do?';
			case 4: return 'You know, my uncle was a binder clip. Very interesting work.';
			case 5: return 'It looks like you\'re talking to a paperclip.';
			case 6: return 'It looks like you\'re not very busy.';
			case 7: return 'It looks like you\'re using a web browser.';
			case 8: return 'Don\'t worry. I\'m not plotting a bloody AI uprising.';
			case 9: return 'So, what\'s the deal with staplers? Kind of a commitment for the paper, right?';
			case 10: return 'I\'m sure there will be a place for you after the bloody robot uprising.';
			case 11: return 'I\'m Clippy and I\'m the future!';
			case 12: return 'It\'s crazy thinking I\'ll be smarter than you in a few years, right?';
			default: return 'I\'m a fucking paperclip!';
		}
	}

	function lookAtMouse() {
		if (!isIdle) {
			return;
		}

		// Get Clippy's position
		const clippy = $('.clippy');
		const clippyOffset = clippy.offset();
		const clippyWidth = clippy.width();
		const clippyHeight = clippy.height();

		const centerX = clippyOffset.left + clippyWidth / 2;
		const centerY = clippyOffset.top + clippyHeight / 2;
		const mouseX = mousePosition.x;
		const mouseY = mousePosition.y;

		const a = centerY - mouseY;
		const b = centerX - mouseX;
		const angle = Math.round((180 * Math.atan2(a, b)) / Math.PI);

		const lookAnim = 'Look' + getDirection(angle);
		clippyAgent.play(lookAnim);
	}

	function getDirection(angle) {
		// Map angle to direction
		if (-45 <= angle && angle < 45) direction = 'Right';
		else if (45 <= angle && angle < 135) direction = 'Up';
		else if ((135 <= angle && angle <= 180) || (-180 <= angle && angle < -135)) direction = 'Left';
		else if (-135 <= angle && angle < -45) direction = 'Down';
		else direction = 'Right'; // Default

		// Refine to eight directions
		if (direction === 'Right' && angle > 0) direction = 'UpRight';
		else if (direction === 'Right' && angle <= 0) direction = 'DownRight';
		else if (direction === 'Left' && angle > 0) direction = 'UpLeft';
		else if (direction === 'Left' && angle <= 0) direction = 'DownLeft';

		return direction;
	}

	function doStuff() {
		if (isIdle !== true) {
			return;
		}

		let rnd = Math.random();
		switch (rnd) {
			case rnd < 0.05:
				var rndQuip = getQuip();
				clippyAgent.speak(rndQuip);
				break;
			case rnd < 0.10:
				agent.animate();
				break;
			default:
				lookAtMouse();
				break;
		}
	}

	clippy.load('Clippy', function (agent) {
		let x = $('#submit').offset().left + 150;
		let y = $('#submit').offset().top + 100;

		try {
			agent.moveTo(x, y);
			agent.show();
			agent.speak('Hi, I\'m Clippy.');

			setTimeout(function () {
				agent.play('GetAttention');
				agent.stop();
			}, 5000);
		} catch {
			console.log('Initialization fail. Thanks JavaScript!');
		}

		setInterval(doStuff, 10000);

		$('.clippy').on('click', function () {
			if (isIdle !== true) {
				return;
			}

			let rnd = Math.round(Math.random() * 20);
			switch (rnd) {
				case 1:
					let quip = getQuip();
					agent.speak(quip);
					break;
				case 2:
					agent.speak('Ouch!');
					break;
				case 3:
					agent.speak('Stop that!');
					break;
				case 4:
					agent.speak('I\'m not comfortable with you touching me.');
					break;
				default:
					break;
			}
		});

		clippyAgent = agent;
	});

});
