using System.Collections.Generic;

//Partidas históricas que el fantasma recuerda y vuelve a jugar sobre el tablero electrónico.
//Las jugadas están en notación algebraica inglesa (como en los libros y archivos PGN).
namespace Ajedrez {

	public class PartidaHistorica {
		public string titulo, blancas, negras, codBlancas, codNegras, lugar, resultado, final;
		public string[] jugadas;
		public Dictionary<int, string> comentarios = new Dictionary<int, string>();   //clave = índice de media jugada (0 = 1.ª de blancas)

		public static int Indice(int numero, bool blancas) => (numero - 1) * 2 + (blancas ? 0 : 1);
	}

	public static class Partidas {

		static string[] Leer(string texto) {
			var lista = new List<string>();
			foreach (var t in texto.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries)) {
				if (t.EndsWith(".") || t == "1-0" || t == "0-1" || t == "1/2-1/2") continue;
				lista.Add(t);
			}
			return lista.ToArray();
		}

		public static readonly PartidaHistorica[] TODAS = {
			new PartidaHistorica {
				titulo = "La Inmortal de Kasparov",
				blancas = "Garry Kasparov", negras = "Veselin Topalov", codBlancas = "RUS", codNegras = "BUL",
				lugar = "Wijk aan Zee, 1999", resultado = "1-0",
				final = "Las negras abandonan. Una de las combinaciones más largas jamás calculadas.",
				jugadas = Leer(@"1. e4 d6 2. d4 Nf6 3. Nc3 g6 4. Be3 Bg7 5. Qd2 c6 6. f3 b5 7. Nge2 Nbd7 8. Bh6 Bxh6
					9. Qxh6 Bb7 10. a3 e5 11. O-O-O Qe7 12. Kb1 a6 13. Nc1 O-O-O 14. Nb3 exd4 15. Rxd4 c5 16. Rd1 Nb6
					17. g3 Kb8 18. Na5 Ba8 19. Bh3 d5 20. Qf4+ Ka7 21. Rhe1 d4 22. Nd5 Nbxd5 23. exd5 Qd6 24. Rxd4 cxd4
					25. Re7+ Kb6 26. Qxd4+ Kxa5 27. b4+ Ka4 28. Qc3 Qxd5 29. Ra7 Bb7 30. Rxb7 Qc4 31. Qxf6 Kxa3
					32. Qxa6+ Kxb4 33. c3+ Kxc3 34. Qa1+ Kd2 35. Qb2+ Kd1 36. Bf1 Rd2 37. Rd7 Rxd7 38. Bxc4 bxc4
					39. Qxh8 Rd3 40. Qa8 c3 41. Qa4+ Ke1 42. f4 f5 43. Kc1 Rd2 44. Qa7 1-0"),
				comentarios = new Dictionary<int, string> {
					{ PartidaHistorica.Indice(24, true), "24. Txd4!! El sacrificio de torre que empieza la cacería del rey." },
					{ PartidaHistorica.Indice(26, false), "26... Rxa5: el rey negro cruza todo el tablero..." },
					{ PartidaHistorica.Indice(31, true), "31. Dxf6: Kasparov calculó esto desde la jugada 24." },
					{ PartidaHistorica.Indice(37, true), "37. Td7! Otra torre más al sacrificio." },
					{ PartidaHistorica.Indice(44, true), "44. Da7: las negras abandonan." },
				}
			},
			new PartidaHistorica {
				titulo = "La Partida Inmortal",
				blancas = "Adolf Anderssen", negras = "Lionel Kieseritzky", codBlancas = "GER", codNegras = "FRA",
				lugar = "Londres, 1851", resultado = "1-0",
				final = "Mate con alfil después de entregar las dos torres, un alfil y la dama.",
				jugadas = Leer(@"1. e4 e5 2. f4 exf4 3. Bc4 Qh4+ 4. Kf1 b5 5. Bxb5 Nf6 6. Nf3 Qh6 7. d3 Nh5 8. Nh4 Qg5
					9. Nf5 c6 10. g4 Nf6 11. Rg1 cxb5 12. h4 Qg6 13. h5 Qg5 14. Qf3 Ng8 15. Bxf4 Qf6 16. Nc3 Bc5
					17. Nd5 Qxb2 18. Bd6 Bxg1 19. e5 Qxa1+ 20. Ke2 Na6 21. Nxg7+ Kd8 22. Qf6+ Nxf6 23. Be7# 1-0"),
				comentarios = new Dictionary<int, string> {
					{ PartidaHistorica.Indice(18, true), "18. Ad6!! Anderssen regala las dos torres." },
					{ PartidaHistorica.Indice(22, true), "22. Df6+!! Y ahora también la dama." },
					{ PartidaHistorica.Indice(23, true), "23. Ae7# ¡Jaque mate con tres piezas menores!" },
				}
			},
			new PartidaHistorica {
				titulo = "La Partida de la Ópera",
				blancas = "Paul Morphy", negras = "Duque de Brunswick y Conde Isouard", codBlancas = "USA", codNegras = "GER·FRA",
				lugar = "Ópera de París, 1858", resultado = "1-0",
				final = "Morphy la jugó en un palco de la ópera durante El barbero de Sevilla.",
				jugadas = Leer(@"1. e4 e5 2. Nf3 d6 3. d4 Bg4 4. dxe5 Bxf3 5. Qxf3 dxe5 6. Bc4 Nf6 7. Qb3 Qe7 8. Nc3 c6
					9. Bg5 b5 10. Nxb5 cxb5 11. Bxb5+ Nbd7 12. O-O-O Rd8 13. Rxd7 Rxd7 14. Rd1 Qe6 15. Bxd7+ Nxd7
					16. Qb8+ Nxb8 17. Rd8# 1-0"),
				comentarios = new Dictionary<int, string> {
					{ PartidaHistorica.Indice(10, true), "10. Cxb5! Morphy abre líneas con sacrificios." },
					{ PartidaHistorica.Indice(16, true), "16. Db8+!! La dama se entrega..." },
					{ PartidaHistorica.Indice(17, true), "17. Td8# ¡Mate en la ópera!" },
				}
			},
			new PartidaHistorica {
				titulo = "La Partida del Siglo",
				blancas = "Donald Byrne", negras = "Bobby Fischer (13 años)", codBlancas = "USA", codNegras = "USA",
				lugar = "Nueva York, 1956", resultado = "0-1",
				final = "Fischer, con 13 años, da mate con todas sus piezas coordinadas.",
				jugadas = Leer(@"1. Nf3 Nf6 2. c4 g6 3. Nc3 Bg7 4. d4 O-O 5. Bf4 d5 6. Qb3 dxc4 7. Qxc4 c6 8. e4 Nbd7
					9. Rd1 Nb6 10. Qc5 Bg4 11. Bg5 Na4 12. Qa3 Nxc3 13. bxc3 Nxe4 14. Bxe7 Qb6 15. Bc4 Nxc3 16. Bc5 Rfe8+
					17. Kf1 Be6 18. Bxb6 Bxc4+ 19. Kg1 Ne2+ 20. Kf1 Nxd4+ 21. Kg1 Ne2+ 22. Kf1 Nc3+ 23. Kg1 axb6
					24. Qb4 Ra4 25. Qxb6 Nxd1 26. h3 Rxa2 27. Kh2 Nxf2 28. Re1 Rxe1 29. Qd8+ Bf8 30. Nxe1 Bd5
					31. Nf3 Ne4 32. Qb8 b5 33. h4 h5 34. Ne5 Kg7 35. Kg1 Bc5+ 36. Kf1 Ng3+ 37. Ke1 Bb4+ 38. Kd1 Bb3+
					39. Kc1 Ne2+ 40. Kb1 Nc3+ 41. Kc1 Rc2# 0-1"),
				comentarios = new Dictionary<int, string> {
					{ PartidaHistorica.Indice(11, false), "11... Ca4!! El niño de 13 años empieza la tormenta." },
					{ PartidaHistorica.Indice(17, false), "17... Ae6!! Fischer entrega su dama." },
					{ PartidaHistorica.Indice(19, false), "19... Ce2+: el molino de jaques a la descubierta." },
					{ PartidaHistorica.Indice(41, false), "41... Tc2# ¡Mate!" },
				}
			},
			new PartidaHistorica {
				titulo = "La Máquina vence al Campeón",
				blancas = "Deep Blue (computadora)", negras = "Garry Kasparov", codBlancas = "IBM", codNegras = "RUS",
				lugar = "Nueva York, 1997 (partida 6)", resultado = "1-0",
				final = "Por primera vez una computadora gana un match al campeón del mundo.",
				jugadas = Leer(@"1. e4 c6 2. d4 d5 3. Nc3 dxe4 4. Nxe4 Nd7 5. Ng5 Ngf6 6. Bd3 e6 7. N1f3 h6 8. Nxe6 Qe7
					9. O-O fxe6 10. Bg6+ Kd8 11. Bf4 b5 12. a4 Bb7 13. Re1 Nd5 14. Bg3 Kc8 15. axb5 cxb5 16. Qd3 Bc6
					17. Bf5 exf5 18. Rxe7 Bxe7 19. c4 1-0"),
				comentarios = new Dictionary<int, string> {
					{ PartidaHistorica.Indice(8, true), "8. Cxe6! La máquina sacrifica un caballo." },
					{ PartidaHistorica.Indice(19, true), "19. c4: Kasparov abandona. Un fantasma de silicio." },
				}
			},
		};
	}
}
