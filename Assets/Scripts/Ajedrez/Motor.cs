using System.Collections.Generic;

//Motor sencillo del "rival fantasma": búsqueda alfa-beta con profundidad fija, búsqueda de capturas
//al final (quiescencia) y una evaluación de material + tablas de posición. Juega con fuerza de club.
namespace Ajedrez {

	public static class Motor {

		static readonly int[] VALOR = { 0, 100, 320, 330, 500, 900, 0 };

		//Tablas de posición desde el punto de vista de las blancas (índice 0 = a1)
		static readonly int[] T_PEON = {
			 0,  0,  0,  0,  0,  0,  0,  0,
			 5, 10, 10,-20,-20, 10, 10,  5,
			 5, -5,-10,  0,  0,-10, -5,  5,
			 0,  0,  0, 20, 20,  0,  0,  0,
			 5,  5, 10, 25, 25, 10,  5,  5,
			10, 10, 20, 30, 30, 20, 10, 10,
			50, 50, 50, 50, 50, 50, 50, 50,
			 0,  0,  0,  0,  0,  0,  0,  0 };
		static readonly int[] T_CABALLO = {
			-50,-40,-30,-30,-30,-30,-40,-50,
			-40,-20,  0,  5,  5,  0,-20,-40,
			-30,  5, 10, 15, 15, 10,  5,-30,
			-30,  0, 15, 20, 20, 15,  0,-30,
			-30,  5, 15, 20, 20, 15,  5,-30,
			-30,  0, 10, 15, 15, 10,  0,-30,
			-40,-20,  0,  0,  0,  0,-20,-40,
			-50,-40,-30,-30,-30,-30,-40,-50 };
		static readonly int[] T_ALFIL = {
			-20,-10,-10,-10,-10,-10,-10,-20,
			-10,  5,  0,  0,  0,  0,  5,-10,
			-10, 10, 10, 10, 10, 10, 10,-10,
			-10,  0, 10, 10, 10, 10,  0,-10,
			-10,  5,  5, 10, 10,  5,  5,-10,
			-10,  0,  5, 10, 10,  5,  0,-10,
			-10,  0,  0,  0,  0,  0,  0,-10,
			-20,-10,-10,-10,-10,-10,-10,-20 };
		static readonly int[] T_TORRE = {
			  0,  0,  0,  5,  5,  0,  0,  0,
			 -5,  0,  0,  0,  0,  0,  0, -5,
			 -5,  0,  0,  0,  0,  0,  0, -5,
			 -5,  0,  0,  0,  0,  0,  0, -5,
			 -5,  0,  0,  0,  0,  0,  0, -5,
			 -5,  0,  0,  0,  0,  0,  0, -5,
			  5, 10, 10, 10, 10, 10, 10,  5,
			  0,  0,  0,  0,  0,  0,  0,  0 };
		static readonly int[] T_DAMA = {
			-20,-10,-10, -5, -5,-10,-10,-20,
			-10,  0,  5,  0,  0,  0,  0,-10,
			-10,  5,  5,  5,  5,  5,  0,-10,
			  0,  0,  5,  5,  5,  5,  0, -5,
			 -5,  0,  5,  5,  5,  5,  0, -5,
			-10,  0,  5,  5,  5,  5,  0,-10,
			-10,  0,  0,  0,  0,  0,  0,-10,
			-20,-10,-10, -5, -5,-10,-10,-20 };
		static readonly int[] T_REY = {
			 20, 30, 10,  0,  0, 10, 30, 20,
			 20, 20,  0,  0,  0,  0, 20, 20,
			-10,-20,-20,-20,-20,-20,-20,-10,
			-20,-30,-30,-40,-40,-30,-30,-20,
			-30,-40,-40,-50,-50,-40,-40,-30,
			-30,-40,-40,-50,-50,-40,-40,-30,
			-30,-40,-40,-50,-50,-40,-40,-30,
			-30,-40,-40,-50,-50,-40,-40,-30 };
		static readonly int[][] TABLAS = { null, T_PEON, T_CABALLO, T_ALFIL, T_TORRE, T_DAMA, T_REY };

		const int MATE = 100000;

		public static int nodos;
		static System.Random azar = new System.Random();

		//Evaluación desde el punto de vista del bando que juega
		public static int Evaluar(Posicion p) {
			int total = 0;
			for (int i = 0; i < 64; i++) {
				int pz = p.c[i];
				if (pz == 0) continue;
				int t = P.Tipo(pz);
				int idx = pz > 0 ? i : (7 - P.Fila(i)) * 8 + P.Columna(i);
				int v = VALOR[t] + TABLAS[t][idx];
				total += pz > 0 ? v : -v;
			}
			return p.blancasJuegan ? total : -total;
		}

		static int Orden(Jugada j) {
			int o = 0;
			if (j.capturada != 0) o += 10 * VALOR[P.Tipo(j.capturada)] - VALOR[P.Tipo(j.pieza)] + 1000;
			if (j.corona != 0) o += VALOR[j.corona];
			return o;
		}

		static void Ordenar(List<Jugada> lista) => lista.Sort((a, b) => Orden(b).CompareTo(Orden(a)));

		//Devuelve la mejor jugada encontrada (o false si no hay jugadas)
		public static bool MejorJugada(Posicion original, int profundidad, out Jugada mejor, int variedad = 15) {
			var p = original.Copia();
			nodos = 0;
			mejor = default;
			var legales = p.Legales();
			if (legales.Count == 0) return false;
			Ordenar(legales);
			int alfa = -MATE - 1;
			var puntajes = new List<(Jugada j, int v)>();
			foreach (var j in legales) {
				var u = p.Hacer(j);
				//Ventana hasta (mejor - variedad - 1): una jugada que la rebasa queda fuera de las candidatas.
				//Sin el -1, una cota igual al umbral se colaba como candidata (el fantasma jugaba al azar).
				int umbral = alfa <= -MATE ? -MATE - 1 : alfa - variedad;
				int v = -Buscar(p, profundidad - 1, -MATE - 1, -umbral + 1, 1);
				p.DeshacerJugada(j, u);
				puntajes.Add((j, v));
				if (v > alfa) alfa = v;
			}
			//Elige al azar entre las jugadas casi tan buenas como la mejor (el fantasma no siempre juega igual)
			var candidatas = puntajes.FindAll(x => x.v >= alfa - variedad);
			mejor = candidatas[azar.Next(candidatas.Count)].j;
			return true;
		}

		static int Buscar(Posicion p, int prof, int alfa, int beta, int ply) {
			nodos++;
			if (prof <= 0) return Quiescencia(p, alfa, beta, 0);
			var jugadas = p.Pseudolegales();
			Ordenar(jugadas);
			bool alguna = false;
			foreach (var j in jugadas) {
				var u = p.Hacer(j);
				int r = p.Rey(!p.blancasJuegan);
				if (r >= 0 && p.Atacada(r, p.blancasJuegan)) {
					p.DeshacerJugada(j, u);
					continue;
				}
				alguna = true;
				int v = -Buscar(p, prof - 1, -beta, -alfa, ply + 1);
				p.DeshacerJugada(j, u);
				if (v >= beta) return v;
				if (v > alfa) alfa = v;
			}
			if (!alguna) return p.EnJaque() ? -MATE + ply : 0;
			return alfa;
		}

		static int Quiescencia(Posicion p, int alfa, int beta, int prof) {
			nodos++;
			int quieto = Evaluar(p);
			if (quieto >= beta) return quieto;
			if (quieto > alfa) alfa = quieto;
			if (prof >= 4) return alfa;
			var jugadas = p.Pseudolegales(true);
			Ordenar(jugadas);
			foreach (var j in jugadas) {
				var u = p.Hacer(j);
				int r = p.Rey(!p.blancasJuegan);
				if (r >= 0 && p.Atacada(r, p.blancasJuegan)) {
					p.DeshacerJugada(j, u);
					continue;
				}
				int v = -Quiescencia(p, -beta, -alfa, prof + 1);
				p.DeshacerJugada(j, u);
				if (v >= beta) return v;
				if (v > alfa) alfa = v;
			}
			return alfa;
		}
	}
}
