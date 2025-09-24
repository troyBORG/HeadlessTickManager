#!/bin/bash
# tickwatch.sh - Pretty viewer for HeadlessTickManager logs
# Usage:
#   ./tickwatch.sh              -> show last 30 relevant lines
#   ./tickwatch.sh -f           -> follow live updates
#   ./tickwatch.sh -H           -> hide headless "join" (world restarts)
#   ./tickwatch.sh -f -H        -> both

logdir="$HOME/.steam/steam/steamapps/common/Resonite/Headless/Logs"

follow=0
hide_host=0
while [ $# -gt 0 ]; do
  case "$1" in
    -f|--follow) follow=1 ;;
    -H|--hide-host) hide_host=1 ;;
    *) ;;
  esac
  shift
done

logfile=$(ls -t "$logdir"/ResoniteHeadless\ *.log 2>/dev/null | head -n 1)
if [ -z "$logfile" ]; then
  echo "No ResoniteHeadless log files found in $logdir"
  exit 1
fi

echo "Newest log: $logfile"
echo
echo "⚡ Tick field meanings:"
echo "   ticks       → current tick rate applied"
echo "   raw         → unsmoothed target tick rate (based on load)"
echo "   ema         → smoothed tick rate (Exponential Moving Average)"
echo "   activeWorlds→ worlds above ActiveWorldUserThreshold"
echo "   joins/min   → recent join rate (used for surge handling)"
echo

# --- Detect the headless account's UserID from the log header, e.g.:
# "Initializing SignalR: UserLogin: U-yourHeadlessId"
HEADLESS_ID=$(grep -m1 -E 'UserLogin:\s*(U-[^ ]+)' "$logfile" | sed -E 's/.*UserLogin:\s*(U-[^ ]+).*/\1/')
# Fallback empty if not found
HEADLESS_ID="${HEADLESS_ID:-}"

# Pass flags/vars into AWK
HH="$hide_host"
HID="$HEADLESS_ID"

read_filter='
BEGIN {
  hide_host = HH
  headless_id = HID
}

{
  line = $0

  # ---------- JOIN ----------
  if (index(line, "User Joined ")>0 && index(line, " Username: ")>0 && index(line, " UserID: ")>0) {
    # world
    s1 = index(line, "User Joined ") + 12
    e1 = index(line, ". Username: ") - 1
    if (e1 >= s1) world = substr(line, s1, e1 - s1 + 1); else world = "?"

    # user (display name)
    s2 = index(line, "Username: ") + 10
    e2 = index(line, ", UserID: ") - 1
    if (e2 >= s2) user = substr(line, s2, e2 - s2 + 1); else user = "?"

    # uid (stop at next comma)
    s3 = index(line, "UserID: ") + 8
    tail = substr(line, s3)
    p = index(tail, ",")
    if (p > 0) e3 = s3 + p - 2; else e3 = length(line)
    if (e3 >= s3) uid = substr(line, s3, e3 - s3 + 1); else uid = "?"

    # Determine if this is the headless account
    is_headless = 0
    if (headless_id != "" && uid == headless_id) {
      is_headless = 1
    } else {
      # Heuristic fallback if we couldn’t detect headless_id
      if (uid ~ /^U-.*-Headless$/) is_headless = 1
      if (user ~ /_Headless$/)     is_headless = 1
    }

    if (is_headless) {
      if (hide_host == 0) {
        printf("🌎 World Restarted (%s)\n", world)
      }
      next
    }

    printf("✅ User Joined (%s): %s (%s)\n", world, user, uid)
    next
  }

  # ---------- LEAVE ----------
  if (index(line, "User Left ")>0 && index(line, " Username: ")>0 && index(line, " UserID: ")>0) {
    # world
    s1 = index(line, "User Left ") + 10
    e1 = index(line, ". Username: ") - 1
    if (e1 >= s1) world = substr(line, s1, e1 - s1 + 1); else world = "?"

    # user
    s2 = index(line, "Username: ") + 10
    e2 = index(line, ", UserID: ") - 1
    if (e2 >= s2) user = substr(line, s2, e2 - s2 + 1); else user = "?"

    # uid
    s3 = index(line, "UserID: ") + 8
    tail = substr(line, s3)
    p = index(tail, ",")
    if (p > 0) e3 = s3 + p - 2; else e3 = length(line)
    if (e3 >= s3) uid = substr(line, s3, e3 - s3 + 1); else uid = "?"

    # Skip headless "leave" lines (cleanup) if it matches detected ID or heuristic
    is_headless = 0
    if (headless_id != "" && uid == headless_id) {
      is_headless = 1
    } else {
      if (uid ~ /^U-.*-Headless$/) is_headless = 1
      if (user ~ /_Headless$/)     is_headless = 1
    }
    if (is_headless) {
      next
    }

    printf("❌ User Left (%s): %s (%s)\n", world, user, uid)
    next
  }

  # ---------- TICK LINES ----------
  # Match old styles (emoji or bracketed tag) OR new plain message format.
  if (index(line, "⚡ ")>0) {
    print substr(line, index(line, "⚡ "))
    next
  }
  if (index(line, "[HeadlessTickManager]")>0) {
    print substr(line, index(line, "[HeadlessTickManager]"))
    next
  }
  # New plain message from TickController: "Applied N ticks (raw=..., ema=..., ...)"
  if (line ~ /Applied [0-9]+ ticks \(raw=[0-9.]+, ema=[0-9.]+, activeWorlds=[0-9]+, joins\/min=[0-9.]+\)/) {
    # Print a compact normalized line
    match(line, /Applied [0-9]+ ticks \(raw=[^)]*\)/)
    if (RSTART > 0) print substr(line, RSTART, RLENGTH)
    next
  }
}
'

if [ "$follow" -eq 1 ]; then
  tail -F "$logfile" | awk -v HH="$HH" -v HID="$HID" "$read_filter"
else
  awk -v HH="$HH" -v HID="$HID" "$read_filter" "$logfile" | tail -n 30
fi
