using System.Collections.Generic;
using System.Text;

//Reglas completas del ajedrez: posición, generación de jugadas legales (enroque, al paso, coronación),
//jaque, mate, ahogado y notación algebraica (SAN) para leer y escribir partidas.
//Piezas: positivo = blancas, negativo = negras. 1 Peón, 2 Caballo, 3 Alfil, 4 Torre, 5 Dama, 6 Rey.
//Casillas: 0 = a1, 7 = h1, 56 = a8, 63 = h8 (fila * 8 + columna).
namespace Ajedrez {

	public static class P {
		public const int VACIO = 0, PEON = 1, CABALLO = 2, ALFIL = 3, TORRE = 4, DAMA = 5, REY = 6;
		public static int Tipo(int pieza) => pieza < 0 ? -pieza : pieza;
		public static bool EsBlanca(int pieza) => pieza > 0;
		public static int Columna(int c) => c & 7;
		public static int Fila(int c) => c >> 3;
		public static int Casilla(int col, int fila) => fila * 8 + col;
		public static string Nombre(int c) => ((char)('a' + Columna(c))).ToString() + (char)('1' + Fila(c));
		public static int DeNombre(string s) => Casilla(s[0] - 'a', s[1] - '1');
		//Letras en español para la planilla (R D T A C) y en inglés para leer PGN (K Q R B N)
		public static readonly string LETRAS_ES = " PCATDR";
		public static readonly string LETRAS_EN = " PNBRQK";
		public static readonly string[] NOMBRES = { "", "peón", "caballo", "alfil", "torre", "dama", "rey" };
	}

	public struct Jugada {
		public int desde, hasta;
		public int pieza;        //pieza que se mueve (con signo)
		public int capturada;    //pieza capturada (con signo) o 0
		public int corona;       //tipo al que corona (sin signo) o 0
		public bool alPaso, enroque;

		public bool EsCaptura => capturada != 0;
		public override string ToString() => P.Nombre(desde) + P.Nombre(hasta) + (corona != 0 ? P.LETRAS_EN[corona].ToString().ToLower() : "");
		public bool Igual(Jugada o) => desde == o.desde && hasta == o.hasta && corona == o.corona;
	}

	struct Deshacer {
		public int enroques, alPaso, medio, capturada, casillaCapturada;
	}

	public class Posicion {

		public readonly int[] c = new int[64];
		public bool blancasJuegan = true;
		public int enroques = 15;      //1 = O-O blancas, 2 = O-O-O blancas, 4 = O-O negras, 8 = O-O-O negras
		public int alPaso = -1;        //casilla donde se puede capturar al paso
		public int medioMov;
		public int numero = 1;

		public const string INICIAL = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

		public static Posicion Inicial() => DesdeFen(INICIAL);

		public Posicion Copia() {
			var p = new Posicion();
			System.Array.Copy(c, p.c, 64);
			p.blancasJuegan = blancasJuegan;
			p.enroques = enroques;
			p.alPaso = alPaso;
			p.medioMov = medioMov;
			p.numero = numero;
			return p;
		}

		// ------------------------------------------------ FEN

		public static Posicion DesdeFen(string fen) {
			var p = new Posicion();
			var partes = fen.Split(' ');
			int fila = 7, col = 0;
			foreach (char ch in partes[0]) {
				if (ch == '/') { fila--; col = 0; continue; }
				if (char.IsDigit(ch)) { col += ch - '0'; continue; }
				int tipo = "pnbrqk".IndexOf(char.ToLower(ch)) + 1;
				p.c[P.Casilla(col, fila)] = char.IsUpper(ch) ? tipo : -tipo;
				col++;
			}
			p.blancasJuegan = partes.Length < 2 || partes[1] == "w";
			p.enroques = 0;
			if (partes.Length > 2) {
				if (partes[2].Contains("K")) p.enroques |= 1;
				if (partes[2].Contains("Q")) p.enroques |= 2;
				if (partes[2].Contains("k")) p.enroques |= 4;
				if (partes[2].Contains("q")) p.enroques |= 8;
			}
			p.alPaso = partes.Length > 3 && partes[3] != "-" ? P.DeNombre(partes[3]) : -1;
			p.medioMov = partes.Length > 4 ? int.Parse(partes[4]) : 0;
			p.numero = partes.Length > 5 ? int.Parse(partes[5]) : 1;
			return p;
		}

		public string Fen() {
			var sb = new StringBuilder();
			for (int fila = 7; fila >= 0; fila--) {
				int vacias = 0;
				for (int col = 0; col < 8; col++) {
					int pz = c[P.Casilla(col, fila)];
					if (pz == 0) { vacias++; continue; }
					if (vacias > 0) { sb.Append(vacias); vacias = 0; }
					char ch = "pnbrqk"[P.Tipo(pz) - 1];
					sb.Append(pz > 0 ? char.ToUpper(ch) : ch);
				}
				if (vacias > 0) sb.Append(vacias);
				if (fila > 0) sb.Append('/');
			}
			string enr = (enroques & 1) != 0 ? "K" : "";
			enr += (enroques & 2) != 0 ? "Q" : "";
			enr += (enroques & 4) != 0 ? "k" : "";
			enr += (enroques & 8) != 0 ? "q" : "";
			if (enr == "") enr = "-";
			return sb + (blancasJuegan ? " w " : " b ") + enr + " " + (alPaso >= 0 ? P.Nombre(alPaso) : "-") + " " + medioMov + " " + numero;
		}

		// ------------------------------------------------ Ataques

		static readonly int[] SALTOS_C = { 6, 10, 15, 17, -6, -10, -15, -17 };
		static readonly int[] DC = { 1, -1, 0, 0, 1, 1, -1, -1 };
		static readonly int[] DF = { 0, 0, 1, -1, 1, -1, 1, -1 };

		static bool Dentro(int col, int fila) => col >= 0 && col < 8 && fila >= 0 && fila < 8;

		//¿La casilla está atacada por el bando indicado?
		public bool Atacada(int casilla, bool porBlancas) {
			int col = P.Columna(casilla), fila = P.Fila(casilla);
			int s = porBlancas ? 1 : -1;
			//Peones
			int fp = fila - s;
			if (fp >= 0 && fp < 8) {
				if (col > 0 && c[P.Casilla(col - 1, fp)] == s * P.PEON) return true;
				if (col < 7 && c[P.Casilla(col + 1, fp)] == s * P.PEON) return true;
			}
			//Caballos
			for (int dc = -2; dc <= 2; dc++) {
				for (int df = -2; df <= 2; df++) {
					if (dc * dc + df * df != 5) continue;
					if (Dentro(col + dc, fila + df) && c[P.Casilla(col + dc, fila + df)] == s * P.CABALLO) return true;
				}
			}
			//Rey
			for (int dc = -1; dc <= 1; dc++) {
				for (int df = -1; df <= 1; df++) {
					if ((dc != 0 || df != 0) && Dentro(col + dc, fila + df) && c[P.Casilla(col + dc, fila + df)] == s * P.REY) return true;
				}
			}
			//Líneas y diagonales
			for (int d = 0; d < 8; d++) {
				int cc = col + DC[d], ff = fila + DF[d];
				while (Dentro(cc, ff)) {
					int pz = c[P.Casilla(cc, ff)];
					if (pz != 0) {
						if ((pz > 0) == porBlancas) {
							int t = P.Tipo(pz);
							if (t == P.DAMA || (d < 4 && t == P.TORRE) || (d >= 4 && t == P.ALFIL)) return true;
						}
						break;
					}
					cc += DC[d];
					ff += DF[d];
				}
			}
			return false;
		}

		public int Rey(bool blanco) {
			int buscado = blanco ? P.REY : -P.REY;
			for (int i = 0; i < 64; i++) if (c[i] == buscado) return i;
			return -1;
		}

		public bool EnJaque() {
			int r = Rey(blancasJuegan);
			return r >= 0 && Atacada(r, !blancasJuegan);
		}

		// ------------------------------------------------ Generación

		public List<Jugada> Pseudolegales(bool soloCapturas = false) {
			var lista = new List<Jugada>(48);
			int s = blancasJuegan ? 1 : -1;
			for (int de = 0; de < 64; de++) {
				int pz = c[de];
				if (pz == 0 || (pz > 0) != blancasJuegan) continue;
				int col = P.Columna(de), fila = P.Fila(de);
				switch (P.Tipo(pz)) {
					case P.PEON: {
						int adelante = fila + s;
						int ultima = blancasJuegan ? 7 : 0;
						if (adelante >= 0 && adelante < 8) {
							int a = P.Casilla(col, adelante);
							if (c[a] == 0 && !soloCapturas) {
								AgregarPeon(lista, de, a, pz, 0, adelante == ultima);
								int inicio = blancasJuegan ? 1 : 6;
								int a2 = P.Casilla(col, fila + 2 * s);
								if (fila == inicio && c[a2] == 0) lista.Add(new Jugada { desde = de, hasta = a2, pieza = pz });
							}
							for (int dc = -1; dc <= 1; dc += 2) {
								if (col + dc < 0 || col + dc > 7) continue;
								int x = P.Casilla(col + dc, adelante);
								if (c[x] != 0 && (c[x] > 0) != blancasJuegan) AgregarPeon(lista, de, x, pz, c[x], adelante == ultima);
								else if (x == alPaso) lista.Add(new Jugada { desde = de, hasta = x, pieza = pz, capturada = -pz, alPaso = true });
							}
						}
						break;
					}
					case P.CABALLO:
						for (int dc = -2; dc <= 2; dc++) {
							for (int df = -2; df <= 2; df++) {
								if (dc * dc + df * df != 5 || !Dentro(col + dc, fila + df)) continue;
								Agregar(lista, de, P.Casilla(col + dc, fila + df), pz, soloCapturas);
							}
						}
						break;
					case P.ALFIL: Deslizar(lista, de, pz, 4, 8, soloCapturas); break;
					case P.TORRE: Deslizar(lista, de, pz, 0, 4, soloCapturas); break;
					case P.DAMA: Deslizar(lista, de, pz, 0, 8, soloCapturas); break;
					case P.REY:
						for (int d = 0; d < 8; d++) {
							if (Dentro(col + DC[d], fila + DF[d])) Agregar(lista, de, P.Casilla(col + DC[d], fila + DF[d]), pz, soloCapturas);
						}
						if (!soloCapturas) Enroques(lista, de, pz);
						break;
				}
			}
			return lista;
		}

		void Agregar(List<Jugada> lista, int de, int a, int pz, bool soloCapturas) {
			int destino = c[a];
			if (destino != 0 && (destino > 0) == (pz > 0)) return;
			if (soloCapturas && destino == 0) return;
			lista.Add(new Jugada { desde = de, hasta = a, pieza = pz, capturada = destino });
		}

		static void AgregarPeon(List<Jugada> lista, int de, int a, int pz, int cap, bool corona) {
			if (!corona) {
				lista.Add(new Jugada { desde = de, hasta = a, pieza = pz, capturada = cap });
				return;
			}
			foreach (int t in new[] { P.DAMA, P.CABALLO, P.TORRE, P.ALFIL }) lista.Add(new Jugada { desde = de, hasta = a, pieza = pz, capturada = cap, corona = t });
		}

		void Deslizar(List<Jugada> lista, int de, int pz, int d0, int d1, bool soloCapturas) {
			int col = P.Columna(de), fila = P.Fila(de);
			for (int d = d0; d < d1; d++) {
				int cc = col + DC[d], ff = fila + DF[d];
				while (Dentro(cc, ff)) {
					int a = P.Casilla(cc, ff);
					if (c[a] == 0) {
						if (!soloCapturas) lista.Add(new Jugada { desde = de, hasta = a, pieza = pz });
					} else {
						if ((c[a] > 0) != (pz > 0)) lista.Add(new Jugada { desde = de, hasta = a, pieza = pz, capturada = c[a] });
						break;
					}
					cc += DC[d];
					ff += DF[d];
				}
			}
		}

		void Enroques(List<Jugada> lista, int de, int pz) {
			bool b = pz > 0;
			int baseF = b ? 0 : 56;
			if (de != baseF + 4) return;
			bool rival = !b;
			int corto = b ? 1 : 4, largo = b ? 2 : 8;
			if ((enroques & corto) != 0 && c[baseF + 5] == 0 && c[baseF + 6] == 0 && c[baseF + 7] == (b ? P.TORRE : -P.TORRE)
				&& !Atacada(baseF + 4, rival) && !Atacada(baseF + 5, rival) && !Atacada(baseF + 6, rival)) {
				lista.Add(new Jugada { desde = de, hasta = baseF + 6, pieza = pz, enroque = true });
			}
			if ((enroques & largo) != 0 && c[baseF + 3] == 0 && c[baseF + 2] == 0 && c[baseF + 1] == 0 && c[baseF] == (b ? P.TORRE : -P.TORRE)
				&& !Atacada(baseF + 4, rival) && !Atacada(baseF + 3, rival) && !Atacada(baseF + 2, rival)) {
				lista.Add(new Jugada { desde = de, hasta = baseF + 2, pieza = pz, enroque = true });
			}
		}

		public List<Jugada> Legales() {
			var res = new List<Jugada>(40);
			foreach (var j in Pseudolegales()) {
				var u = Hacer(j);
				int r = Rey(!blancasJuegan);
				if (r < 0 || !Atacada(r, blancasJuegan)) res.Add(j);
				DeshacerJugada(j, u);
			}
			return res;
		}

		public List<Jugada> LegalesDesde(int casilla) {
			var res = new List<Jugada>();
			foreach (var j in Legales()) if (j.desde == casilla) res.Add(j);
			return res;
		}

		// ------------------------------------------------ Hacer / deshacer

		internal Deshacer Hacer(Jugada j) {
			var u = new Deshacer { enroques = enroques, alPaso = alPaso, medio = medioMov, capturada = j.capturada, casillaCapturada = j.hasta };
			bool b = j.pieza > 0;
			if (j.alPaso) {
				u.casillaCapturada = j.hasta + (b ? -8 : 8);
				c[u.casillaCapturada] = 0;
			}
			c[j.hasta] = j.corona != 0 ? (b ? j.corona : -j.corona) : j.pieza;
			c[j.desde] = 0;
			if (j.enroque) {
				int baseF = b ? 0 : 56;
				if (j.hasta == baseF + 6) { c[baseF + 5] = c[baseF + 7]; c[baseF + 7] = 0; }
				else { c[baseF + 3] = c[baseF]; c[baseF] = 0; }
			}
			//Derechos de enroque
			if (P.Tipo(j.pieza) == P.REY) enroques &= b ? ~3 : ~12;
			if (j.desde == 0 || j.hasta == 0) enroques &= ~2;
			if (j.desde == 7 || j.hasta == 7) enroques &= ~1;
			if (j.desde == 56 || j.hasta == 56) enroques &= ~8;
			if (j.desde == 63 || j.hasta == 63) enroques &= ~4;
			alPaso = P.Tipo(j.pieza) == P.PEON && System.Math.Abs(j.hasta - j.desde) == 16 ? (j.desde + j.hasta) / 2 : -1;
			medioMov = P.Tipo(j.pieza) == P.PEON || j.capturada != 0 ? 0 : medioMov + 1;
			if (!b) numero++;
			blancasJuegan = !blancasJuegan;
			return u;
		}

		internal void DeshacerJugada(Jugada j, Deshacer u) {
			blancasJuegan = !blancasJuegan;
			bool b = j.pieza > 0;
			if (!b) numero--;
			c[j.desde] = j.pieza;
			c[j.hasta] = 0;
			if (u.capturada != 0) c[u.casillaCapturada] = u.capturada;
			if (j.enroque) {
				int baseF = b ? 0 : 56;
				if (j.hasta == baseF + 6) { c[baseF + 7] = c[baseF + 5]; c[baseF + 5] = 0; }
				else { c[baseF] = c[baseF + 3]; c[baseF + 3] = 0; }
			}
			enroques = u.enroques;
			alPaso = u.alPaso;
			medioMov = u.medio;
		}

		//Aplica una jugada de forma definitiva
		public void Aplicar(Jugada j) => Hacer(j);

		// ------------------------------------------------ Estado de la partida

		public enum Estado { EnJuego, Jaque, Mate, Ahogado, MaterialInsuficiente, Regla50 }

		public Estado Situacion() {
			bool jaque = EnJaque();
			if (Legales().Count == 0) return jaque ? Estado.Mate : Estado.Ahogado;
			if (medioMov >= 100) return Estado.Regla50;
			if (MaterialInsuficiente()) return Estado.MaterialInsuficiente;
			return jaque ? Estado.Jaque : Estado.EnJuego;
		}

		bool MaterialInsuficiente() {
			int menores = 0;
			for (int i = 0; i < 64; i++) {
				int t = P.Tipo(c[i]);
				if (t == P.PEON || t == P.TORRE || t == P.DAMA) return false;
				if (t == P.CABALLO || t == P.ALFIL) menores++;
			}
			return menores <= 1;
		}

		// ------------------------------------------------ Notación algebraica

		//SAN en inglés (como en los libros y PGN): Nf3, exd5, O-O, e8=Q+
		public string San(Jugada j, bool espanol = false) {
			string letras = espanol ? P.LETRAS_ES : P.LETRAS_EN;
			string s;
			int t = P.Tipo(j.pieza);
			if (j.enroque) {
				s = P.Columna(j.hasta) == 6 ? "O-O" : "O-O-O";
			} else if (t == P.PEON) {
				s = j.EsCaptura ? ((char)('a' + P.Columna(j.desde))) + "x" + P.Nombre(j.hasta) : P.Nombre(j.hasta);
				if (j.corona != 0) s += "=" + letras[j.corona];
			} else {
				s = letras[t].ToString();
				//Desambiguación
				bool otraMisma = false, mismaCol = false, mismaFila = false;
				foreach (var o in Legales()) {
					if (o.hasta != j.hasta || o.desde == j.desde || o.pieza != j.pieza) continue;
					otraMisma = true;
					if (P.Columna(o.desde) == P.Columna(j.desde)) mismaCol = true;
					if (P.Fila(o.desde) == P.Fila(j.desde)) mismaFila = true;
				}
				if (otraMisma) {
					if (!mismaCol) s += (char)('a' + P.Columna(j.desde));
					else if (!mismaFila) s += (char)('1' + P.Fila(j.desde));
					else s += P.Nombre(j.desde);
				}
				if (j.EsCaptura) s += "x";
				s += P.Nombre(j.hasta);
			}
			var u = Hacer(j);
			if (EnJaque()) s += Legales().Count == 0 ? "#" : "+";
			DeshacerJugada(j, u);
			return s;
		}

		static string Limpiar(string san) => san.Replace("+", "").Replace("#", "").Replace("!", "").Replace("?", "").Replace("0-0-0", "O-O-O").Replace("0-0", "O-O").Trim();

		//Busca la jugada legal que corresponde a una notación SAN (en inglés)
		public bool DesdeSan(string san, out Jugada jugada) {
			string buscado = Limpiar(san);
			foreach (var j in Legales()) {
				if (Limpiar(San(j)) == buscado) {
					jugada = j;
					return true;
				}
			}
			jugada = default;
			return false;
		}

		// ------------------------------------------------ Perft (para pruebas)

		public long Perft(int profundidad) {
			if (profundidad == 0) return 1;
			long n = 0;
			foreach (var j in Pseudolegales()) {
				var u = Hacer(j);
				int r = Rey(!blancasJuegan);
				if (r < 0 || !Atacada(r, blancasJuegan)) n += Perft(profundidad - 1);
				DeshacerJugada(j, u);
			}
			return n;
		}
	}
}
