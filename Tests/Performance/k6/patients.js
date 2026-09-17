// Deterministic synthetic patients, so that every store gets the same data and searches know what exists.
export const FAMILIES = [
  "Hansen",
  "Johansen",
  "Olsen",
  "Larsen",
  "Andersen",
  "Pedersen",
  "Nilsen",
  "Kristiansen",
  "Jensen",
  "Karlsen",
  "Johnsen",
  "Pettersen",
  "Eriksen",
  "Berg",
  "Haugen",
  "Hagen",
  "Johannessen",
  "Andreassen",
  "Jacobsen",
  "Dahl",
  "Jørgensen",
  "Halvorsen",
  "Henriksen",
  "Lund",
  "Sørensen",
  "Losen",
  "Moen",
  "Gundersen",
  "Iversen",
  "Strand",
  "Solberg",
  "Svendsen",
  "Eide",
  "Knutsen",
  "Martinsen",
  "Paulsen",
  "Bakken",
  "Kristoffersen",
  "Mathisen",
  "Lie",
  "Amundsen",
  "Nguyen",
  "Rasmussen",
  "Ali",
  "Lunde",
  "Solheim",
  "Berge",
  "Moe",
  "Nygård",
  "Bakke",
];
export const GIVENS = [
  "Jan",
  "Per",
  "Bjørn",
  "Ole",
  "Kristian",
  "Kjell",
  "Knut",
  "Arne",
  "Svein",
  "Thomas",
  "Anne",
  "Inger",
  "Kari",
  "Marit",
  "Ingrid",
  "Liv",
  "Skodde",
  "Berit",
  "Astrid",
  "Bjørg",
  "Hans",
  "Geir",
  "Tor",
  "Morten",
  "Terje",
  "Olav",
  "Erik",
  "Los",
  "Andreas",
  "John",
  "Randi",
  "Solveig",
  "Hilde",
  "Anna",
  "Audun",
  "Elisabeth",
  "Marianne",
  "Ida",
  "Ragnhild",
  "Tone",
];

// A small integer hash so that values look random but are reproducible.
function mix(i, salt) {
  let x = (i * 2654435761 + salt * 40503) >>> 0;
  x ^= x >>> 16;
  x = Math.imul(x, 2246822507) >>> 0;
  x ^= x >>> 13;
  x = Math.imul(x, 3266489909) >>> 0;
  x ^= x >>> 16;
  return x >>> 0;
}

export function patientValues(i) {
  const day = new Date(Date.UTC(1920, 0, 1) + (mix(i, 1) % 36500) * 86400000);
  return {
    family:
      FAMILIES[mix(i, 2) % FAMILIES.length] +
      (mix(i, 3) % 4 === 0 ? "-" + FAMILIES[mix(i, 4) % FAMILIES.length] : ""),
    given: GIVENS[mix(i, 5) % GIVENS.length],
    gender: mix(i, 6) % 2 === 0 ? "female" : "male",
    birthDate: day.toISOString().slice(0, 10),
    identifier: String(10000000000 + i),
  };
}

export function patient(i) {
  const v = patientValues(i);
  return {
    resourceType: "Patient",
    active: true,
    identifier: [
      { system: "urn:oid:2.16.578.1.12.4.1.4.1", value: v.identifier },
    ],
    name: [{ use: "official", family: v.family, given: [v.given] }],
    gender: v.gender,
    birthDate: v.birthDate,
    telecom: [
      {
        system: "phone",
        value: "+47 " + (40000000 + (mix(i, 7) % 9999999)),
        use: "mobile",
      },
    ],
    address: [
      {
        line: ["Storgata " + (1 + (mix(i, 8) % 200))],
        city: "Tromsø",
        postalCode: String(9000 + (mix(i, 9) % 100)),
        country: "NO",
      },
    ],
  };
}
