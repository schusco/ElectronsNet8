function updateScoreboard(ab) {
    const homeScoreEl = document.getElementById(`homescore-${ab.gameId}`);
    const awayScoreEl = document.getElementById(`awayscore-${ab.gameId}`);
    const ball1El = document.getElementById(`ball1-${ab.gameId}`);
    const ball2El = document.getElementById(`ball2-${ab.gameId}`);
    const ball3El = document.getElementById(`ball3-${ab.gameId}`);
    const strike1El = document.getElementById(`strike1-${ab.gameId}`);
    const strike21El = document.getElementById(`strike2-${ab.gameId}`);
    const out1El = document.getElementById(`out1-${ab.gameId}`);
    const out2El = document.getElementById(`out2-${ab.gameId}`);

    if (ball1El) {
        ball1El.className = ab.balls > 0 ? "ballDot" : "noBallDot";
    }
    if (ball2El) {
        ball2El.className = ab.balls > 1 ? "ballDot" : "noBallDot";
    }
    if (ball3El) {
        ball3El.className = ab.balls > 2 ? "ballDot" : "noBallDot";
    }
    if (strike1El) {
        strike1El.className = ab.strikes > 0 ? "strikeDot" : "noStrikeDot";
    }
    if (strike21El) {
        strike21El.className = ab.strikes > 1 ? "strikeDot" : "noStrikeDot";
    }
    if (out1El) {
        out1El.className = (ab.outs == 1 || ab.outs == 2) ? "outDot" : "noOutDot";
    }
    if (out2El) {
        out2El.className = ab.outs == 2 ? "outDot" : "noOutDot";
    }
    const inningEl = document.getElementById(`inningnumber-${ab.gameId}`);
    const upCaret = document.getElementById("caret-up");
    const downCaret = document.getElementById("caret-down");
    if (homeScoreEl && awayScoreEl) {
        homeScoreEl.innerText = ab.score.home;
        awayScoreEl.innerText = ab.score.away;
    }
    if (inningEl) {
        inningEl.innerText = ab.inning.number;
    }
    if (ab.inning && ab.inning.top) {
        upCaret?.classList.remove("d-none");
        downCaret?.classList.add("d-none");
    } else if (ab.inning && !ab.inning.top) {
        downCaret?.classList.remove("d-none");
        upCaret?.classList.add("d-none");
    }
    try { updateRunnersOnDisplay(ab.onBase); }
    catch (e) {
        console.error(`update to update on base display`, e);
    }
}
function updateRunnersOnDisplay(onBase) {
    const firstBase = document.getElementById("base-1");
    const secondBase = document.getElementById("base-2");
    const thirdBase = document.getElementById("base-3");
    // .toggle(className, booleanValue) adds the class if true, removes it if false
    if (firstBase) firstBase.classList.toggle("occupied", (onBase & 1) !== 0);
    if (secondBase) secondBase.classList.toggle("occupied", (onBase & 2) !== 0);
    if (thirdBase) thirdBase.classList.toggle("occupied", (onBase & 4) !== 0);
}

function updateAbData(ab) {
    const abResultEl = document.getElementById('abresult');
    const pitcherEl = document.getElementById('currentPitcher');
    if (abResultEl) {
        abResultEl.innerText = ab.result;
    }
    if (pitcherEl) {
        pitcherEl.innerText = ab.pitching ? ` ${ab.pitching} pitching` : "";
    }
    const pitchLogo = document.querySelector('#pitcherLogo img');
    const hitLogo = document.querySelector('#hitterLogo img');
    const awayTeam = document.getElementById('awayTeamName').innerHTML.replace(/\s+/g, '').toLowerCase();
    const homeTeam = document.getElementById('homeTeamName').innerHTML.replace(/\s+/g, '').toLowerCase();
    const homeLogo = `/Content/images/logos/nextOuting_${homeTeam}.png`
    const awayLogo = `/Content/images/logos/nextOuting_${awayTeam}.png`
    if (pitchLogo && hitLogo) {
        if (ab.inning.top) {
            pitchLogo.src = homeLogo;
            hitLogo.src = awayLogo;
        }
        else {
            pitchLogo.src = awayLogo
            hitLogo.src = homeLogo
        }
    }
    const pitchList = document.querySelector('#pitches ol');
    if (pitchList) {
        pitchList.innerHTML = "";
        const fragment = document.createDocumentFragment();
        ab.pitches.forEach(pitch => {
            const li = document.createElement("li");
            li.className = "mb-1";
            li.textContent = pitch;
            fragment.appendChild(li);
        });
        pitchList.appendChild(fragment);
    }
    const abList = document.querySelector('#prevabs ul');
    if (abList) {
        abList.innerHTML = "";
        const fragment = document.createDocumentFragment();
        ab.inning.previousAbs.forEach(ab => {
            const li = document.createElement("li");
            li.className = "mb-1";
            li.textContent = ab;
            fragment.appendChild(li);
        });
        abList.appendChild(fragment);
    }

}