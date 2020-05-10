/*
 * @(#)Z80.java 1.1 27/04/97 Adam Davidson & Andrew Pollard
 */

/**
 * <p>The Z80 class emulates the Zilog Z80 microprocessor.</p>
 *
 * @version 1.1 27 Apr 1997
 * @author <A HREF="http://www.odie.demon.co.uk/spectrum">Adam Davidson & Andrew Pollard</A>
 *
 * @see Jasper
 * @see Spectrum
 */

namespace Casper {

    public class Z80 {

        public Z80(double clockFrequencyInMHz) {
            tstatesPerInterrupt = (int)((clockFrequencyInMHz * 1e6) / 50);
        }

        protected int tstatesPerInterrupt = 0;

        public const int IM0 = 0;
        public const int IM1 = 1;
        public const int IM2 = 2;

        private const int F_C = 0x01;
        private const int F_N = 0x02;
        private const int F_PV = 0x04;
        private const int F_3 = 0x08;
        private const int F_H = 0x10;
        private const int F_5 = 0x20;
        private const int F_Z = 0x40;
        private const int F_S = 0x80;

        private static readonly bool[] parity = new bool[256];
        static Z80() {
            for (int i = 0; i < 256; i++) {
                bool p = true;
                for (int j = i; j > 0; j >>= 1) {
                    if ((j&1) != 0) {
                        p = !p;
                    }
                }
                parity[i] = p;
            }
        }


        /** Main registers */
        protected int _A = 0, _HL = 0, _B = 0, _C = 0, _DE = 0;
        protected bool fS = false, fZ = false, f5 = false, fH = false;
        protected bool f3 = false, fPV = false, fN = false, fC = false;

        /** Alternate registers */
        protected int _AF_ = 0, _HL_ = 0, _BC_ = 0, _DE_ = 0;

        /** Index registers - ID used as temporary for ix/iy */
        protected int _IX = 0, _IY = 0, _ID = 0;

        /** Stack Pointer and Program Counter */
        protected int _SP = 0, _PC = 0;

        /** Interrupt and Refresh registers */
        protected int _I = 0, _R = 0, _R7 = 0;

        /** Interrupt flip-flops */
        protected bool _IFF1 = true, _IFF2 = true;
        protected int _IM = 2;

        /** Memory */
        public readonly int[] mem = new int[65536];

        /** 16 bit register access */
        public int AF() { return (A() << 8) | F(); }
        public void AF(int word) {
            A(word >> 8);
            F(word & 0xff);
        }

        public int BC() { return (B() << 8) | C(); }
        public void BC(int word) {
            B(word >> 8);
            C(word & 0xff);
        }

        public int DE() { return _DE; }
        public void DE(int word) {
            _DE = word;
        }

        public int HL() { return _HL; }
        public void HL(int word) {
            _HL = word;
        }

        public int PC() { return _PC; }
        public void PC(int word) {
            _PC = word;
        }

        public int SP() { return _SP; }
        public void SP(int word) {
            _SP = word;
        }

        public int ID() { return _ID; }
        public void ID(int word) {
            _ID = word;
        }

        public int IX() { return _IX; }
        public void IX(int word) {
            _IX = word;
        }

        public int IY() { return _IY; }
        public void IY(int word) {
            _IY = word;
        }


        /** 8 bit register access */
        public int A() { return _A; }
        public void A(int bite) {
            _A = bite;
        }

        public int F() {
            return (Sset() ? F_S : 0) |
                (Zset() ? F_Z : 0) |
                (f5 ? F_5 : 0) |
                (Hset() ? F_H : 0) |
                (f3 ? F_3 : 0) |
                (PVset() ? F_PV : 0) |
                (Nset() ? F_N : 0) |
                (Cset() ? F_C : 0);
        }
        public void F(int bite) {
            fS = (bite & F_S) != 0;
            fZ = (bite & F_Z) != 0;
            f5 = (bite & F_5) != 0;
            fH = (bite & F_H) != 0;
            f3 = (bite & F_3) != 0;
            fPV = (bite & F_PV) != 0;
            fN = (bite & F_N) != 0;
            fC = (bite & F_C) != 0;
        }


        public int B() { return _B; }
        public void B(int bite) {
            _B = bite;
        }
        public int C() { return _C; }
        public void C(int bite) {
            _C = bite;
        }

        public int D() { return (_DE >> 8); }
        public void D(int bite) {
            _DE = (bite << 8) | (_DE & 0x00ff);
        }
        public int E() { return (_DE & 0xff); }
        public void E(int bite) {
            _DE = (_DE & 0xff00) | bite;
        }

        public int H() { return (_HL >> 8); }
        public void H(int bite) {
            _HL = (bite << 8) | (_HL & 0x00ff);
        }
        public int L() { return (_HL & 0xff); }
        public void L(int bite) {
            _HL = (_HL & 0xff00) | bite;
        }

        public int IDH() { return (_ID >> 8); }
        public void IDH(int bite) {
            _ID = (bite << 8) | (_ID & 0x00ff);
        }
        public int IDL() { return (_ID & 0xff); }
        public void IDL(int bite) {
            _ID = (_ID & 0xff00) | bite;
        }


        /** Memory refresh register */
        public int R7() { return _R7; }
        public int R() { return (_R & 0x7f) | _R7; }
        public void R(int bite) {
            _R = bite;
            _R7 = bite & 0x80;
        }

        public void REFRESH(int t) {
            _R += t;
        }


        /** Interrupt modes/register */
        public int I() { return _I; }
        public void I(int bite) {
            _I = bite;
        }

        public bool IFF1() { return _IFF1; }
        public void IFF1(bool iff1) {
            _IFF1 = iff1;
        }

        public bool IFF2() { return _IFF2; }
        public void IFF2(bool iff2) {
            _IFF2 = iff2;
        }

        public int IM() { return _IM; }
        public void IM(int im) {
            _IM = im;
        }


        /** Flag access */
        public void setZ(bool f) { fZ = f; }
        public void setC(bool f) { fC = f; }
        public void setS(bool f) { fS = f; }
        public void setH(bool f) { fH = f; }
        public void setN(bool f) { fN = f; }
        public void setPV(bool f) { fPV = f; }
        public void set3(bool f) { f3 = f; }
        public void set5(bool f) { f5 = f; }

        public bool Zset() { return fZ; }
        public bool Cset() { return fC; }
        public bool Sset() { return fS; }
        public bool Hset() { return fH; }
        public bool Nset() { return fN; }
        public bool PVset() { return fPV; }


        /** sbyte access */
        private int peekb(int addr) {
            return mem[addr];
        }
        public virtual void pokeb(int addr, int newsbyte) {
            mem[addr] = newsbyte;
        }


        /** Word access */
        public virtual void pokew(int addr, int word) {
            pokeb(addr, word & 0xff);
            addr++;
            pokeb(addr & 0xffff, word >> 8);
        }
        private int peekw(int addr) {
            int t = peekb(addr);
            addr++;
            return t | (peekb(addr & 0xffff) << 8);
        }


        /** Index register access */
        private int ID_d() {
            return ((ID() + (sbyte)nxtpcb()) & 0xffff);
        }


        /** Stack access */
        public void pushw(int word) {
            int sp = ((SP() - 2) & 0xffff);
            SP(sp);
            pokew(sp, word);
        }
        public int popw() {
            int sp = SP();
            int t = peekb(sp);
            sp++;
            t |= (peekb(sp & 0xffff) << 8);
            SP(++sp & 0xffff);
            return t;
        }


        /** Call stack */
        public void pushpc() { pushw(PC()); }
        public void poppc() { PC(popw()); }


        /** Program access */
        private int nxtpcb() {
            int pc = PC();
            int t = peekb(pc);
            PC(++pc & 0xffff);
            return t;
        }
        private int nxtpcw() {
            int pc = PC();
            int t = peekb(pc);
            t |= (peekb(++pc & 0xffff) << 8);
            PC(++pc & 0xffff);
            return t;
        }


        public virtual void Step() {
            execute();
        }

        /** Reset all registers to power on state */
        public virtual void Reset() {
            PC(0);
            SP(0);

            A(0);
            F(0);
            BC(0);
            DE(0);
            HL(0);

            exx();
            ex_af_af();

            A(0);
            F(0);
            BC(0);
            DE(0);
            HL(0);

            IX(0);
            IY(0);

            R(0);

            I(0);
            IFF1(false);
            IFF2(false);
            IM(IM0);
        }


        /** IO ports */
        public virtual void outb(int port, int bite, int tstates) {
        }
        public virtual int inb(int port) {
            return 0xff;
        }

        /** Interrupt handlers */
        private static bool interruptTriggered(int tstates) {
            return (tstates >= 0);
        }

        public virtual int interrupt() {
            // If not a non-maskable interrupt
            if (!IFF1()) {
                return 0;
            }

            switch (IM()) {
            case IM0:
            case IM1:
                pushpc();
                IFF1(false);
                IFF2(false);
                PC(56);
                return 13;
            case IM2:
                pushpc();
                IFF1(false);
                IFF2(false);
                int t = (I() << 8) | 0x00ff;
                PC(peekw(t));
                return 19;
            }

            return 0;
        }


        int local_tstates;

        /** Z80 fetch/execute loop */
        void execute() {
            local_tstates -= tstatesPerInterrupt;

            while (true) {

                if (interruptTriggered(local_tstates)) {
                    local_tstates -= interrupt();
                    return;
                }

                REFRESH(1);

                switch (nxtpcb()) {

                case 0:    /* NOP */
                {
                        local_tstates += (4);
                        break;
                    }
                case 8:    /* EX AF,AF' */
                {
                        ex_af_af();
                        local_tstates += (4);
                        break;
                    }
                case 16:    /* DJNZ dis */
                {
                        int b;

                        B(b = qdec8(B()));
                        if (b != 0) {
                            sbyte d = (sbyte)nxtpcb();
                            PC((PC() + d) & 0xffff);
                            local_tstates += (13);
                        }
                        else {
                            PC(inc16(PC()));
                            local_tstates += (8);
                        }
                        break;
                    }
                case 24: /* JR dis */
                {
                        sbyte d = (sbyte)nxtpcb();
                        PC((PC() + d) & 0xffff);
                        local_tstates += (12);
                        break;
                    }
                /* JR cc,dis */
                case 32:    /* JR NZ,dis */
                {
                        if (!Zset()) {
                            sbyte d = (sbyte)nxtpcb();
                            PC((PC() + d) & 0xffff);
                            local_tstates += (12);
                        }
                        else {
                            PC(inc16(PC()));
                            local_tstates += (7);
                        }
                        break;
                    }
                case 40:    /* JR Z,dis */
                {
                        if (Zset()) {
                            sbyte d = (sbyte)nxtpcb();
                            PC((PC() + d) & 0xffff);
                            local_tstates += (12);
                        }
                        else {
                            PC(inc16(PC()));
                            local_tstates += (7);
                        }
                        break;
                    }
                case 48:    /* JR NC,dis */
                {
                        if (!Cset()) {
                            sbyte d = (sbyte)nxtpcb();
                            PC((PC() + d) & 0xffff);
                            local_tstates += (12);
                        }
                        else {
                            PC(inc16(PC()));
                            local_tstates += (7);
                        }
                        break;
                    }
                case 56:    /* JR C,dis */
                {
                        if (Cset()) {
                            sbyte d = (sbyte)nxtpcb();
                            PC((PC() + d) & 0xffff);
                            local_tstates += (12);
                        }
                        else {
                            PC(inc16(PC()));
                            local_tstates += (7);
                        }
                        break;
                    }

                /* LD rr,nn / ADD HL,rr */
                case 1:    /* LD BC(),nn */
                {
                        BC(nxtpcw());
                        local_tstates += (10);
                        break;
                    }
                case 9:    /* ADD HL,BC */
                {
                        HL(add16(HL(), BC()));
                        local_tstates += (11);
                        break;
                    }
                case 17:    /* LD DE,nn */
                {
                        DE(nxtpcw());
                        local_tstates += (10);
                        break;
                    }
                case 25:    /* ADD HL,DE */
                {
                        HL(add16(HL(), DE()));
                        local_tstates += (11);
                        break;
                    }
                case 33:    /* LD HL,nn */
                {
                        HL(nxtpcw());
                        local_tstates += (10);
                        break;
                    }
                case 41:    /* ADD HL,HL */
                {
                        int hl = HL();
                        HL(add16(hl, hl));
                        local_tstates += (11);
                        break;
                    }
                case 49:    /* LD SP,nn */
                {
                        SP(nxtpcw());
                        local_tstates += (10);
                        break;
                    }
                case 57:    /* ADD HL,SP */
                {
                        HL(add16(HL(), SP()));
                        local_tstates += (11);
                        break;
                    }

                /* LD (**),A/A,(**) */
                case 2:    /* LD (BC),A */
                { pokeb(BC(), A()); local_tstates += (7); break; }
                case 10:    /* LD A,(BC) */
                { A(peekb(BC())); local_tstates += (7); break; }
                case 18:    /* LD (DE),A */
                { pokeb(DE(), A()); local_tstates += (7); break; }
                case 26:    /* LD A,(DE) */
                { A(peekb(DE())); local_tstates += (7); break; }
                case 34:    /* LD (nn),HL */
                { pokew(nxtpcw(), HL()); local_tstates += (16); break; }
                case 42:    /* LD HL,(nn) */
                { HL(peekw(nxtpcw())); local_tstates += (16); break; }
                case 50:    /* LD (nn),A */
                { pokeb(nxtpcw(), A()); local_tstates += (13); break; }
                case 58:    /* LD A,(nn) */
                { A(peekb(nxtpcw())); local_tstates += (13); break; }

                /* INC/DEC * */
                case 3:    /* INC BC */
                { BC(inc16(BC())); local_tstates += (6); break; }
                case 11:    /* DEC BC */
                { BC(dec16(BC())); local_tstates += (6); break; }
                case 19:    /* INC DE */
                { DE(inc16(DE())); local_tstates += (6); break; }
                case 27:    /* DEC DE */
                { DE(dec16(DE())); local_tstates += (6); break; }
                case 35:    /* INC HL */
                { HL(inc16(HL())); local_tstates += (6); break; }
                case 43:    /* DEC HL */
                { HL(dec16(HL())); local_tstates += (6); break; }
                case 51:    /* INC SP */
                { SP(inc16(SP())); local_tstates += (6); break; }
                case 59:    /* DEC SP */
                { SP(dec16(SP())); local_tstates += (6); break; }

                /* INC * */
                case 4:    /* INC B */
                { B(inc8(B())); local_tstates += (4); break; }
                case 12:    /* INC C */
                { C(inc8(C())); local_tstates += (4); break; }
                case 20:    /* INC D */
                { D(inc8(D())); local_tstates += (4); break; }
                case 28:    /* INC E */
                { E(inc8(E())); local_tstates += (4); break; }
                case 36:    /* INC H */
                { H(inc8(H())); local_tstates += (4); break; }
                case 44:    /* INC L */
                { L(inc8(L())); local_tstates += (4); break; }
                case 52:    /* INC (HL) */
                {
                        int hl = HL();
                        pokeb(hl, inc8(peekb(hl)));
                        local_tstates += (11);
                        break;
                    }
                case 60:    /* INC A() */
                { A(inc8(A())); local_tstates += (4); break; }

                /* DEC * */
                case 5:    /* DEC B */
                { B(dec8(B())); local_tstates += (4); break; }
                case 13:    /* DEC C */
                { C(dec8(C())); local_tstates += (4); break; }
                case 21:    /* DEC D */
                { D(dec8(D())); local_tstates += (4); break; }
                case 29:    /* DEC E */
                { E(dec8(E())); local_tstates += (4); break; }
                case 37:    /* DEC H */
                { H(dec8(H())); local_tstates += (4); break; }
                case 45:    /* DEC L */
                { L(dec8(L())); local_tstates += (4); break; }
                case 53:    /* DEC (HL) */
                {
                        int hl = HL();
                        pokeb(hl, dec8(peekb(hl)));
                        local_tstates += (11);
                        break;
                    }
                case 61:    /* DEC A() */
                { A(dec8(A())); local_tstates += (4); break; }

                /* LD *,N */
                case 6:    /* LD B,n */
                { B(nxtpcb()); local_tstates += (7); break; }
                case 14:    /* LD C,n */
                { C(nxtpcb()); local_tstates += (7); break; }
                case 22:    /* LD D,n */
                { D(nxtpcb()); local_tstates += (7); break; }
                case 30:    /* LD E,n */
                { E(nxtpcb()); local_tstates += (7); break; }
                case 38:    /* LD H,n */
                { H(nxtpcb()); local_tstates += (7); break; }
                case 46:    /* LD L,n */
                { L(nxtpcb()); local_tstates += (7); break; }
                case 54:    /* LD (HL),n */
                {
                        pokeb(HL(), nxtpcb());
                        local_tstates += (10);
                        break;
                    }
                case 62:    /* LD A,n */
                { A(nxtpcb()); local_tstates += (7); break; }

                /* R**A */
                case 7: /* RLCA */
                { rlc_a(); local_tstates += (4); break; }
                case 15: /* RRCA */
                { rrc_a(); local_tstates += (4); break; }
                case 23: /* RLA */
                { rl_a(); local_tstates += (4); break; }
                case 31: /* RRA */
                { rr_a(); local_tstates += (4); break; }
                case 39: /* DAA */
                { daa_a(); local_tstates += (4); break; }
                case 47: /* CPL */
                { cpl_a(); local_tstates += (4); break; }
                case 55: /* SCF */
                { scf(); local_tstates += (4); break; }
                case 63: /* CCF */
                { ccf(); local_tstates += (4); break; }

                /* LD B,* */
                case 64:    /* LD B,B */
                { local_tstates += (4); break; }
                case 65:    /* LD B,C */
                { B(C()); local_tstates += (4); break; }
                case 66:    /* LD B,D */
                { B(D()); local_tstates += (4); break; }
                case 67:    /* LD B,E */
                { B(E()); local_tstates += (4); break; }
                case 68:    /* LD B,H */
                { B(H()); local_tstates += (4); break; }
                case 69:    /* LD B,L */
                { B(L()); local_tstates += (4); break; }
                case 70:    /* LD B,(HL) */
                { B(peekb(HL())); local_tstates += (7); break; }
                case 71:    /* LD B,A */
                { B(A()); local_tstates += (4); break; }

                /* LD C,* */
                case 72:    /* LD C,B */
                { C(B()); local_tstates += (4); break; }
                case 73:    /* LD C,C */
                { local_tstates += (4); break; }
                case 74:    /* LD C,D */
                { C(D()); local_tstates += (4); break; }
                case 75:    /* LD C,E */
                { C(E()); local_tstates += (4); break; }
                case 76:    /* LD C,H */
                { C(H()); local_tstates += (4); break; }
                case 77:    /* LD C,L */
                { C(L()); local_tstates += (4); break; }
                case 78:    /* LD C,(HL) */
                { C(peekb(HL())); local_tstates += (7); break; }
                case 79:    /* LD C,A */
                { C(A()); local_tstates += (4); break; }

                /* LD D,* */
                case 80:    /* LD D,B */
                { D(B()); local_tstates += (4); break; }
                case 81:    /* LD D,C */
                { D(C()); local_tstates += (4); break; }
                case 82:    /* LD D,D */
                { local_tstates += (4); break; }
                case 83:    /* LD D,E */
                { D(E()); local_tstates += (4); break; }
                case 84:    /* LD D,H */
                { D(H()); local_tstates += (4); break; }
                case 85:    /* LD D,L */
                { D(L()); local_tstates += (4); break; }
                case 86:    /* LD D,(HL) */
                { D(peekb(HL())); local_tstates += (7); break; }
                case 87:    /* LD D,A */
                { D(A()); local_tstates += (4); break; }

                /* LD E,* */
                case 88:    /* LD E,B */
                { E(B()); local_tstates += (4); break; }
                case 89:    /* LD E,C */
                { E(C()); local_tstates += (4); break; }
                case 90:    /* LD E,D */
                { E(D()); local_tstates += (4); break; }
                case 91:    /* LD E,E */
                { local_tstates += (4); break; }
                case 92:    /* LD E,H */
                { E(H()); local_tstates += (4); break; }
                case 93:    /* LD E,L */
                { E(L()); local_tstates += (4); break; }
                case 94:    /* LD E,(HL) */
                { E(peekb(HL())); local_tstates += (7); break; }
                case 95:    /* LD E,A */
                { E(A()); local_tstates += (4); break; }

                /* LD H,* */
                case 96:    /* LD H,B */
                { H(B()); local_tstates += (4); break; }
                case 97:    /* LD H,C */
                { H(C()); local_tstates += (4); break; }
                case 98:    /* LD H,D */
                { H(D()); local_tstates += (4); break; }
                case 99:    /* LD H,E */
                { H(E()); local_tstates += (4); break; }
                case 100: /* LD H,H */
                { local_tstates += (4); break; }
                case 101:    /* LD H,L */
                { H(L()); local_tstates += (4); break; }
                case 102:    /* LD H,(HL) */
                { H(peekb(HL())); local_tstates += (7); break; }
                case 103:    /* LD H,A */
                { H(A()); local_tstates += (4); break; }

                /* LD L,* */
                case 104:    /* LD L,B */
                { L(B()); local_tstates += (4); break; }
                case 105:    /* LD L,C */
                { L(C()); local_tstates += (4); break; }
                case 106:    /* LD L,D */
                { L(D()); local_tstates += (4); break; }
                case 107:    /* LD L,E */
                { L(E()); local_tstates += (4); break; }
                case 108:    /* LD L,H */
                { L(H()); local_tstates += (4); break; }
                case 109:    /* LD L,L */
                { local_tstates += (4); break; }
                case 110:    /* LD L,(HL) */
                { L(peekb(HL())); local_tstates += (7); break; }
                case 111:    /* LD L,A */
                { L(A()); local_tstates += (4); break; }

                /* LD (HL),* */
                case 112:    /* LD (HL),B */
                { pokeb(HL(), B()); local_tstates += (7); break; }
                case 113:    /* LD (HL),C */
                { pokeb(HL(), C()); local_tstates += (7); break; }
                case 114:    /* LD (HL),D */
                { pokeb(HL(), D()); local_tstates += (7); break; }
                case 115:    /* LD (HL),E */
                { pokeb(HL(), E()); local_tstates += (7); break; }
                case 116:    /* LD (HL),H */
                { pokeb(HL(), H()); local_tstates += (7); break; }
                case 117:    /* LD (HL),L */
                { pokeb(HL(), L()); local_tstates += (7); break; }
                case 118:    /* HALT */
                {
                        int haltsToInterrupt = (((-local_tstates - 1) / 4) + 1);
                        local_tstates += (haltsToInterrupt * 4);
                        REFRESH(haltsToInterrupt - 1);
                        break;
                    }
                case 119:    /* LD (HL),A */
                { pokeb(HL(), A()); local_tstates += (7); break; }

                /* LD A,* */
                case 120:    /* LD A,B */
                { A(B()); local_tstates += (4); break; }
                case 121:    /* LD A,C */
                { A(C()); local_tstates += (4); break; }
                case 122:    /* LD A,D */
                { A(D()); local_tstates += (4); break; }
                case 123:    /* LD A,E */
                { A(E()); local_tstates += (4); break; }
                case 124:    /* LD A,H */
                { A(H()); local_tstates += (4); break; }
                case 125:    /* LD A,L */
                { A(L()); local_tstates += (4); break; }
                case 126:    /* LD A,(HL) */
                { A(peekb(HL())); local_tstates += (7); break; }
                case 127:    /* LD A,A */
                { local_tstates += (4); break; }

                /* ADD A,* */
                case 128:    /* ADD A,B */
                { add_a(B()); local_tstates += (4); break; }
                case 129:    /* ADD A,C */
                { add_a(C()); local_tstates += (4); break; }
                case 130:    /* ADD A,D */
                { add_a(D()); local_tstates += (4); break; }
                case 131:    /* ADD A,E */
                { add_a(E()); local_tstates += (4); break; }
                case 132:    /* ADD A,H */
                { add_a(H()); local_tstates += (4); break; }
                case 133:    /* ADD A,L */
                { add_a(L()); local_tstates += (4); break; }
                case 134:    /* ADD A,(HL) */
                { add_a(peekb(HL())); local_tstates += (7); break; }
                case 135:    /* ADD A,A */
                { add_a(A()); local_tstates += (4); break; }

                /* ADC A,* */
                case 136:    /* ADC A,B */
                { adc_a(B()); local_tstates += (4); break; }
                case 137:    /* ADC A,C */
                { adc_a(C()); local_tstates += (4); break; }
                case 138:    /* ADC A,D */
                { adc_a(D()); local_tstates += (4); break; }
                case 139:    /* ADC A,E */
                { adc_a(E()); local_tstates += (4); break; }
                case 140:    /* ADC A,H */
                { adc_a(H()); local_tstates += (4); break; }
                case 141:    /* ADC A,L */
                { adc_a(L()); local_tstates += (4); break; }
                case 142:    /* ADC A,(HL) */
                { adc_a(peekb(HL())); local_tstates += (7); break; }
                case 143:    /* ADC A,A */
                { adc_a(A()); local_tstates += (4); break; }

                /* SUB * */
                case 144:    /* SUB B */
                { sub_a(B()); local_tstates += (4); break; }
                case 145:    /* SUB C */
                { sub_a(C()); local_tstates += (4); break; }
                case 146:    /* SUB D */
                { sub_a(D()); local_tstates += (4); break; }
                case 147:    /* SUB E */
                { sub_a(E()); local_tstates += (4); break; }
                case 148:    /* SUB H */
                { sub_a(H()); local_tstates += (4); break; }
                case 149:    /* SUB L */
                { sub_a(L()); local_tstates += (4); break; }
                case 150:    /* SUB (HL) */
                { sub_a(peekb(HL())); local_tstates += (7); break; }
                case 151:    /* SUB A() */
                { sub_a(A()); local_tstates += (4); break; }

                /* SBC A,* */
                case 152:    /* SBC A,B */
                { sbc_a(B()); local_tstates += (4); break; }
                case 153:    /* SBC A,C */
                { sbc_a(C()); local_tstates += (4); break; }
                case 154:    /* SBC A,D */
                { sbc_a(D()); local_tstates += (4); break; }
                case 155:    /* SBC A,E */
                { sbc_a(E()); local_tstates += (4); break; }
                case 156:    /* SBC A,H */
                { sbc_a(H()); local_tstates += (4); break; }
                case 157:    /* SBC A,L */
                { sbc_a(L()); local_tstates += (4); break; }
                case 158:    /* SBC A,(HL) */
                { sbc_a(peekb(HL())); local_tstates += (7); break; }
                case 159:    /* SBC A,A */
                { sbc_a(A()); local_tstates += (4); break; }

                /* AND * */
                case 160:    /* AND B */
                { and_a(B()); local_tstates += (4); break; }
                case 161:    /* AND C */
                { and_a(C()); local_tstates += (4); break; }
                case 162:    /* AND D */
                { and_a(D()); local_tstates += (4); break; }
                case 163:    /* AND E */
                { and_a(E()); local_tstates += (4); break; }
                case 164:    /* AND H */
                { and_a(H()); local_tstates += (4); break; }
                case 165:    /* AND L */
                { and_a(L()); local_tstates += (4); break; }
                case 166:    /* AND (HL) */
                { and_a(peekb(HL())); local_tstates += (7); break; }
                case 167:    /* AND A() */
                { and_a(A()); local_tstates += (4); break; }

                /* XOR * */
                case 168:    /* XOR B */
                { xor_a(B()); local_tstates += (4); break; }
                case 169:    /* XOR C */
                { xor_a(C()); local_tstates += (4); break; }
                case 170:    /* XOR D */
                { xor_a(D()); local_tstates += (4); break; }
                case 171:    /* XOR E */
                { xor_a(E()); local_tstates += (4); break; }
                case 172:    /* XOR H */
                { xor_a(H()); local_tstates += (4); break; }
                case 173:    /* XOR L */
                { xor_a(L()); local_tstates += (4); break; }
                case 174:    /* XOR (HL) */
                { xor_a(peekb(HL())); local_tstates += (7); break; }
                case 175:    /* XOR A() */
                { xor_a(A()); local_tstates += (4); break; }

                /* OR * */
                case 176:    /* OR B */
                { or_a(B()); local_tstates += (4); break; }
                case 177:    /* OR C */
                { or_a(C()); local_tstates += (4); break; }
                case 178:    /* OR D */
                { or_a(D()); local_tstates += (4); break; }
                case 179:    /* OR E */
                { or_a(E()); local_tstates += (4); break; }
                case 180:    /* OR H */
                { or_a(H()); local_tstates += (4); break; }
                case 181:    /* OR L */
                { or_a(L()); local_tstates += (4); break; }
                case 182:    /* OR (HL) */
                { or_a(peekb(HL())); local_tstates += (7); break; }
                case 183:    /* OR A() */
                { or_a(A()); local_tstates += (4); break; }

                /* CP * */
                case 184:    /* CP B */
                { cp_a(B()); local_tstates += (4); break; }
                case 185:    /* CP C */
                { cp_a(C()); local_tstates += (4); break; }
                case 186:    /* CP D */
                { cp_a(D()); local_tstates += (4); break; }
                case 187:    /* CP E */
                { cp_a(E()); local_tstates += (4); break; }
                case 188:    /* CP H */
                { cp_a(H()); local_tstates += (4); break; }
                case 189:    /* CP L */
                { cp_a(L()); local_tstates += (4); break; }
                case 190:    /* CP (HL) */
                { cp_a(peekb(HL())); local_tstates += (7); break; }
                case 191:    /* CP A() */
                { cp_a(A()); local_tstates += (4); break; }

                /* RET cc */
                case 192:    /* RET NZ */
                {
                        if (!Zset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 200:    /* RET Z */
                {
                        if (Zset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 208:    /* RET NC */
                {
                        if (!Cset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 216:    /* RET C */
                {
                        if (Cset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 224:    /* RET PO */
                {
                        if (!PVset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 232:    /* RET PE */
                {
                        if (PVset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 240:    /* RET P */
                {
                        if (!Sset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }
                case 248:    /* RET M */
                {
                        if (Sset()) {
                            poppc();
                            local_tstates += (11);
                        }
                        else {
                            local_tstates += (5);
                        }
                        break;
                    }

                /* POP,Various */
                case 193:    /* POP BC */
                { BC(popw()); local_tstates += (10); break; }
                case 201: /* RET */
                { poppc(); local_tstates += (10); break; }
                case 209:    /* POP DE */
                { DE(popw()); local_tstates += (10); break; }
                case 217:    /* EXX */
                {
                        exx();
                        local_tstates += (4);
                        break;
                    }
                case 225:    /* POP HL */
                { HL(popw()); local_tstates += (10); break; }
                case 233: /* JP (HL) */
                { PC(HL()); local_tstates += (4); break; }
                case 241:    /* POP AF */
                { AF(popw()); local_tstates += (10); break; }
                case 249:    /* LD SP,HL */
                { SP(HL()); local_tstates += (6); break; }

                /* JP cc,nn */
                case 194:    /* JP NZ,nn */
                {
                        if (!Zset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 202:    /* JP Z,nn */
                {
                        if (Zset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 210:    /* JP NC,nn */
                {
                        if (!Cset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 218:    /* JP C,nn */
                {
                        if (Cset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 226:    /* JP PO,nn */
                {
                        if (!PVset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 234:    /* JP PE,nn */
                {
                        if (PVset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 242:    /* JP P,nn */
                {
                        if (!Sset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }
                case 250:    /* JP M,nn */
                {
                        if (Sset()) {
                            PC(nxtpcw());
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                        }
                        local_tstates += (10);
                        break;
                    }


                /* Various */
                case 195:    /* JP nn */
                { PC(peekw(PC())); local_tstates += (10); break; }
                case 203:    /* prefix CB */
                { local_tstates += execute_cb(); break; }
                case 211:    /* OUT (n),A */
                {
                        outb(nxtpcb(), A(), local_tstates);
                        local_tstates += (11);
                        break;
                    }
                case 219:    /* IN A,(n) */
                {
                        A(inb((A() << 8) | nxtpcb()));
                        local_tstates += (11);
                        break;
                    }
                case 227:    /* EX (SP),HL */
                {
                        int t = HL();
                        int sp = SP();
                        HL(peekw(sp));
                        pokew(sp, t);
                        local_tstates += (19);
                        break;
                    }
                case 235:    /* EX DE,HL */
                {
                        int t = HL();
                        HL(DE());
                        DE(t);
                        local_tstates += (4);
                        break;
                    }
                case 243:    /* DI */
                {
                        IFF1(false);
                        IFF2(false);
                        local_tstates += (4);
                        break;
                    }
                case 251:    /* EI */
                {
                        IFF1(true);
                        IFF2(true);
                        local_tstates += (4);
                        break;
                    }

                /* CALL cc,nn */
                case 196: /* CALL NZ,nn */
                {
                        if (!Zset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 204: /* CALL Z,nn */
                {
                        if (Zset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 212: /* CALL NC,nn */
                {
                        if (!Cset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 220: /* CALL C,nn */
                {
                        if (Cset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 228: /* CALL PO,nn */
                {
                        if (!PVset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 236: /* CALL PE,nn */
                {
                        if (PVset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 244: /* CALL P,nn */
                {
                        if (!Sset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }
                case 252: /* CALL M,nn */
                {
                        if (Sset()) {
                            int t = nxtpcw();
                            pushpc();
                            PC(t);
                            local_tstates += (17);
                        }
                        else {
                            PC((PC() + 2) & 0xffff);
                            local_tstates += (10);
                        }
                        break;
                    }

                /* PUSH,Various */
                case 197:    /* PUSH BC */
                { pushw(BC()); local_tstates += (11); break; }
                case 205:    /* CALL nn */
                {
                        int t = nxtpcw();
                        pushpc();
                        PC(t);
                        local_tstates += (17);
                        break;
                    }
                case 213:    /* PUSH DE */
                { pushw(DE()); local_tstates += (11); break; }
                case 221:    /* prefix IX */
                {
                        ID(IX());
                        local_tstates += execute_id();
                        IX(ID());
                        break;
                    }
                case 229:    /* PUSH HL */
                { pushw(HL()); local_tstates += (11); break; }
                case 237:    /* prefix ED */
                { local_tstates += execute_ed(local_tstates); break; }
                case 245:    /* PUSH AF */
                { pushw(AF()); local_tstates += (11); break; }
                case 253:    /* prefix IY */
                {
                        ID(IY());
                        local_tstates += execute_id();
                        IY(ID());
                        break;
                    }

                /* op A,N */
                case 198: /* ADD A,N */
                { add_a(nxtpcb()); local_tstates += (7); break; }
                case 206: /* ADC A,N */
                { adc_a(nxtpcb()); local_tstates += (7); break; }
                case 214: /* SUB N */
                { sub_a(nxtpcb()); local_tstates += (7); break; }
                case 222: /* SBC A,N */
                { sbc_a(nxtpcb()); local_tstates += (7); break; }
                case 230: /* AND N */
                { and_a(nxtpcb()); local_tstates += (7); break; }
                case 238: /* XOR N */
                { xor_a(nxtpcb()); local_tstates += (7); break; }
                case 246: /* OR N */
                { or_a(nxtpcb()); local_tstates += (7); break; }
                case 254: /* CP N */
                { cp_a(nxtpcb()); local_tstates += (7); break; }

                /* RST n */
                case 199:    /* RST 0 */
                { pushpc(); PC(0); local_tstates += (11); break; }
                case 207:    /* RST 8 */
                { pushpc(); PC(8); local_tstates += (11); break; }
                case 215:    /* RST 16 */
                { pushpc(); PC(16); local_tstates += (11); break; }
                case 223:    /* RST 24 */
                { pushpc(); PC(24); local_tstates += (11); break; }
                case 231:    /* RST 32 */
                { pushpc(); PC(32); local_tstates += (11); break; }
                case 239:    /* RST 40 */
                { pushpc(); PC(40); local_tstates += (11); break; }
                case 247:    /* RST 48 */
                { pushpc(); PC(48); local_tstates += (11); break; }
                case 255:    /* RST 56 */
                { pushpc(); PC(56); local_tstates += (11); break; }

                }

            } // end while
        }


        private int execute_ed(int local_tstates) {

            REFRESH(1);

            switch (nxtpcb()) {

            case 0:  /* NOP */
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 38:
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:

            case 127:
            case 128:
            case 129:
            case 130:
            case 131:
            case 132:
            case 133:
            case 134:
            case 135:
            case 136:
            case 137:
            case 138:
            case 139:
            case 140:
            case 141:
            case 142:
            case 143:
            case 144:
            case 145:
            case 146:
            case 147:
            case 148:
            case 149:
            case 150:
            case 151:
            case 152:
            case 153:
            case 154:
            case 155:
            case 156:
            case 157:
            case 158:
            case 159:

            case 164:
            case 165:
            case 166:
            case 167:

            case 172:
            case 173:
            case 174:
            case 175:

            case 180:
            case 181:
            case 182:
            case 183: {
                    return (8);
                }

            /* IN r,(c) */
            case 64:  /* IN B,(c) */
            { B(in_bc()); return (12); }
            case 72:  /* IN C,(c) */
            { C(in_bc()); return (12); }
            case 80:  /* IN D,(c) */
            { D(in_bc()); return (12); }
            case 88:  /* IN E,(c) */
            { E(in_bc()); return (12); }
            case 96:  /* IN H,(c) */
            { H(in_bc()); return (12); }
            case 104:  /* IN L,(c) */
            { L(in_bc()); return (12); }
            case 112:  /* IN (c) */
            { in_bc(); return (12); }
            case 120:  /* IN A,(c) */
            { A(in_bc()); return (12); }

            /* OUT (c),r */
            case 65:  /* OUT (c),B */
            { outb(BC(), B(), local_tstates); return (12); }
            case 73:  /* OUT (c),C */
            { outb(BC(), C(), local_tstates); return (12); }
            case 81:  /* OUT (c),D */
            { outb(BC(), D(), local_tstates); return (12); }
            case 89:  /* OUT (c),E */
            { outb(BC(), E(), local_tstates); return (12); }
            case 97:  /* OUT (c),H */
            { outb(BC(), H(), local_tstates); return (12); }
            case 105:  /* OUT (c),L */
            { outb(BC(), L(), local_tstates); return (12); }
            case 113:  /* OUT (c),0 */
            { outb(BC(), 0, local_tstates); return (12); }
            case 121:  /* OUT (c),A */
            { outb(BC(), A(), local_tstates); return (12); }

            /* SBC/ADC HL,ss */
            case 66:  /* SBC HL,BC */
            { HL(sbc16(HL(), BC())); return (15); }
            case 74:  /* ADC HL,BC */
            { HL(adc16(HL(), BC())); return (15); }
            case 82:  /* SBC HL,DE */
            { HL(sbc16(HL(), DE())); return (15); }
            case 90:  /* ADC HL,DE */
            { HL(adc16(HL(), DE())); return (15); }
            case 98:  /* SBC HL,HL */
            {
                    int hl = HL();
                    HL(sbc16(hl, hl));
                    return (15);
                }
            case 106:  /* ADC HL,HL */
            {
                    int hl = HL();
                    HL(adc16(hl, hl));
                    return (15);
                }
            case 114:  /* SBC HL,SP */
            { HL(sbc16(HL(), SP())); return (15); }
            case 122:  /* ADC HL,SP */
            { HL(adc16(HL(), SP())); return (15); }

            /* LD (nn),ss, LD ss,(nn) */
            case 67:  /* LD (nn),BC */
            { pokew(nxtpcw(), BC()); return (20); }
            case 75:  /* LD BC(),(nn) */
            { BC(peekw(nxtpcw())); return (20); }
            case 83:  /* LD (nn),DE */
            { pokew(nxtpcw(), DE()); return (20); }
            case 91:  /* LD DE,(nn) */
            { DE(peekw(nxtpcw())); return (20); }
            case 99:  /* LD (nn),HL */
            { pokew(nxtpcw(), HL()); return (20); }
            case 107:  /* LD HL,(nn) */
            { HL(peekw(nxtpcw())); return (20); }
            case 115:  /* LD (nn),SP */
            { pokew(nxtpcw(), SP()); return (20); }
            case 123:  /* LD SP,(nn) */
            { SP(peekw(nxtpcw())); return (20); }

            /* NEG */
            case 68:  /* NEG */
            case 76:  /* NEG */
            case 84:  /* NEG */
            case 92:  /* NEG */
            case 100:  /* NEG */
            case 108:  /* NEG */
            case 116:  /* NEG */
            case 124:  /* NEG */
            { neg_a(); return (8); }

            /* RETn */
            case 69:  /* RETN */
            case 85:  /* RETN */
            case 101:  /* RETN */
            case 117:  /* RETN */
            {
                    IFF1(IFF2());
                    poppc();
                    return (14);
                }
            case 77:  /* RETI */
            case 93:  /* RETI */
            case 109:  /* RETI */
            case 125:  /* RETI */
            {
                    poppc();
                    return (14);
                }

            /* IM x */
            case 70:  /* IM 0 */
            case 78:  /* IM 0 */
            case 102:  /* IM 0 */
            case 110:  /* IM 0 */
            { IM(IM0); return (8); }
            case 86:  /* IM 1 */
            case 118:  /* IM 1 */
            { IM(IM1); return (8); }
            case 94:  /* IM 2 */
            case 126:  /* IM 2 */
            { IM(IM2); return (8); }

            /* LD A,s / LD s,A / RxD */
            case 71:  /* LD I,A */
            { I(A()); return (9); }
            case 79:  /* LD R,A */
            { R(A()); return (9); }
            case 87:  /* LD A,I */
            { ld_a_i(); return (9); }
            case 95:  /* LD A,R */
            { ld_a_r(); return (9); }
            case 103:  /* RRD */
            { rrd_a(); return (18); }
            case 111:  /* RLD */
            { rld_a(); return (18); }

            /* xxI */
            case 160:  /* LDI */
            {
                    pokeb(DE(), peekb(HL()));
                    DE(inc16(DE()));
                    HL(inc16(HL()));
                    BC(dec16(BC()));

                    setPV(BC() != 0);
                    setH(false);
                    setN(false);

                    return (16);
                }
            case 161:  /* CPI */
            {
                    bool c = Cset();

                    cp_a(peekb(HL()));
                    HL(inc16(HL()));
                    BC(dec16(BC()));

                    setPV(BC() != 0);
                    setC(c);

                    return (16);
                }
            case 162:  /* INI */
            {
                    int b;
                    pokeb(HL(), inb(BC()));
                    B(b = qdec8(B()));
                    HL(inc16(HL()));

                    setZ(b == 0);
                    setN(true);

                    return (16);
                }
            case 163:  /* OUTI */
            {
                    int b;
                    B(b = qdec8(B()));
                    outb(BC(), peekb(HL()), local_tstates);
                    HL(inc16(HL()));

                    setZ(b == 0);
                    setN(true);

                    return (16);
                }

            /* xxD */
            case 168:  /* LDD */
            {
                    pokeb(DE(), peekb(HL()));
                    DE(dec16(DE()));
                    HL(dec16(HL()));
                    BC(dec16(BC()));

                    setPV(BC() != 0);
                    setH(false);
                    setN(false);

                    return (16);
                }
            case 169:  /* CPD */
            {
                    bool c = Cset();

                    cp_a(peekb(HL()));
                    HL(dec16(HL()));
                    BC(dec16(BC()));

                    setPV(BC() != 0);
                    setC(c);

                    return (16);
                }
            case 170:  /* IND */
            {
                    int b;
                    pokeb(HL(), inb(BC()));
                    B(b = qdec8(B()));
                    HL(dec16(HL()));

                    setZ(b == 0);
                    setN(true);

                    return (16);
                }
            case 171:  /* OUTD */
            {
                    int b;
                    B(b = qdec8(B()));
                    outb(BC(), peekb(HL()), local_tstates);
                    HL(dec16(HL()));

                    setZ(b == 0);
                    setN(true);

                    return (16);
                }

            /* xxIR */
            case 176:  /* LDIR */
            {
                    int _local_tstates = 0;
                    int count, dest, from;

                    count = BC();
                    dest = DE();
                    from = HL();
                    REFRESH(-2);
                    do {
                        pokeb(dest, peekb(from));
                        from = inc16(from);
                        dest = inc16(dest);
                        count = dec16(count);

                        _local_tstates += (21);
                        REFRESH(2);
                        if (interruptTriggered(_local_tstates)) {
                            break;
                        }
                    } while (count != 0);
                    if (count != 0) {
                        PC((PC() - 2) & 0xffff);
                        setH(false);
                        setN(false);
                        setPV(true);
                    }
                    else {
                        _local_tstates += (-5);
                        setH(false);
                        setN(false);
                        setPV(false);
                    }
                    DE(dest);
                    HL(from);
                    BC(count);

                    return (_local_tstates);
                }
            case 177:  /* CPIR */
            {
                    bool c = Cset();

                    cp_a(peekb(HL()));
                    HL(inc16(HL()));
                    BC(dec16(BC()));

                    bool pv = (BC() != 0);

                    setPV(pv);
                    setC(c);
                    if (pv && !Zset()) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }
            case 178:  /* INIR */
            {
                    int b;
                    pokeb(HL(), inb(BC()));
                    B(b = qdec8(B()));
                    HL(inc16(HL()));

                    setZ(true);
                    setN(true);
                    if (b != 0) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }
            case 179:  /* OTIR */
            {
                    int b;
                    B(b = qdec8(B()));
                    outb(BC(), peekb(HL()), local_tstates);
                    HL(inc16(HL()));

                    setZ(true);
                    setN(true);
                    if (b != 0) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }

            /* xxDR */
            case 184:  /* LDDR */
            {
                    int _local_tstates = 0;
                    int count, dest, from;

                    count = BC();
                    dest = DE();
                    from = HL();
                    REFRESH(-2);
                    do {
                        pokeb(dest, peekb(from));
                        from = dec16(from);
                        dest = dec16(dest);
                        count = dec16(count);

                        _local_tstates += (21);
                        REFRESH(2);
                        if (interruptTriggered(_local_tstates)) {
                            break;
                        }
                    } while (count != 0);
                    if (count != 0) {
                        PC((PC() - 2) & 0xffff);
                        setH(false);
                        setN(false);
                        setPV(true);
                    }
                    else {
                        _local_tstates += (-5);
                        setH(false);
                        setN(false);
                        setPV(false);
                    }
                    DE(dest);
                    HL(from);
                    BC(count);

                    return (_local_tstates);
                }
            case 185:  /* CPDR */
            {
                    bool c = Cset();

                    cp_a(peekb(HL()));
                    HL(dec16(HL()));
                    BC(dec16(BC()));

                    bool pv = (BC() != 0);

                    setPV(pv);
                    setC(c);
                    if (pv && !Zset()) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }
            case 186:  /* INDR */
            {
                    int b;
                    pokeb(HL(), inb(BC()));
                    B(b = qdec8(B()));
                    HL(dec16(HL()));

                    setZ(true);
                    setN(true);
                    if (b != 0) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }
            case 187:  /* OTDR */
            {
                    int b;
                    B(b = qdec8(B()));
                    outb(BC(), peekb(HL()), local_tstates);
                    HL(dec16(HL()));

                    setZ(true);
                    setN(true);
                    if (b != 0) {
                        PC((PC() - 2) & 0xffff);
                        return (21);
                    }
                    return (16);
                }

            } // end switch

            // NOP
            return (8);
        }

        private int execute_cb() {
            REFRESH(1);

            switch (nxtpcb()) {

            case 0: /* RLC B */
            { B(rlc(B())); return (8); }
            case 1: /* RLC C */
            { C(rlc(C())); return (8); }
            case 2: /* RLC D */
            { D(rlc(D())); return (8); }
            case 3: /* RLC E */
            { E(rlc(E())); return (8); }
            case 4: /* RLC H */
            { H(rlc(H())); return (8); }
            case 5: /* RLC L */
            { L(rlc(L())); return (8); }
            case 6: /* RLC (HL) */
            {
                    int hl = HL();
                    pokeb(hl, rlc(peekb(hl)));
                    return (15);
                }
            case 7: /* RLC A */
            { A(rlc(A())); return (8); }

            case 8: /* RRC B */
            { B(rrc(B())); return (8); }
            case 9: /* RRC C */
            { C(rrc(C())); return (8); }
            case 10:    /* RRC D */
            { D(rrc(D())); return (8); }
            case 11:    /* RRC E */
            { E(rrc(E())); return (8); }
            case 12:    /* RRC H */
            { H(rrc(H())); return (8); }
            case 13:    /* RRC L */
            { L(rrc(L())); return (8); }
            case 14:    /* RRC (HL) */
            {
                    int hl = HL();
                    pokeb(hl, rrc(peekb(hl)));
                    return (15);
                }
            case 15:    /* RRC A */
            { A(rrc(A())); return (8); }

            case 16:    /* RL B */
            { B(rl(B())); return (8); }
            case 17:    /* RL C */
            { C(rl(C())); return (8); }
            case 18:    /* RL D */
            { D(rl(D())); return (8); }
            case 19:    /* RL E */
            { E(rl(E())); return (8); }
            case 20:    /* RL H */
            { H(rl(H())); return (8); }
            case 21:    /* RL L */
            { L(rl(L())); return (8); }
            case 22:    /* RL (HL) */
            {
                    int hl = HL();
                    pokeb(hl, rl(peekb(hl)));
                    return (15);
                }
            case 23:    /* RL A */
            { A(rl(A())); return (8); }

            case 24:    /* RR B */
            { B(rr(B())); return (8); }
            case 25:    /* RR C */
            { C(rr(C())); return (8); }
            case 26:    /* RR D */
            { D(rr(D())); return (8); }
            case 27:    /* RR E */
            { E(rr(E())); return (8); }
            case 28:    /* RR H */
            { H(rr(H())); return (8); }
            case 29:    /* RR L */
            { L(rr(L())); return (8); }
            case 30:    /* RR (HL) */
            {
                    int hl = HL();
                    pokeb(hl, rr(peekb(hl)));
                    return (15);
                }
            case 31:    /* RR A */
            { A(rr(A())); return (8); }

            case 32:    /* SLA B */
            { B(sla(B())); return (8); }
            case 33:    /* SLA C */
            { C(sla(C())); return (8); }
            case 34:    /* SLA D */
            { D(sla(D())); return (8); }
            case 35:    /* SLA E */
            { E(sla(E())); return (8); }
            case 36:    /* SLA H */
            { H(sla(H())); return (8); }
            case 37:    /* SLA L */
            { L(sla(L())); return (8); }
            case 38:    /* SLA (HL) */
            {
                    int hl = HL();
                    pokeb(hl, sla(peekb(hl)));
                    return (15);
                }
            case 39:    /* SLA A */
            { A(sla(A())); return (8); }

            case 40:    /* SRA B */
            { B(sra(B())); return (8); }
            case 41:    /* SRA C */
            { C(sra(C())); return (8); }
            case 42:    /* SRA D */
            { D(sra(D())); return (8); }
            case 43:    /* SRA E */
            { E(sra(E())); return (8); }
            case 44:    /* SRA H */
            { H(sra(H())); return (8); }
            case 45:    /* SRA L */
            { L(sra(L())); return (8); }
            case 46:    /* SRA (HL) */
            {
                    int hl = HL();
                    pokeb(hl, sra(peekb(hl)));
                    return (15);
                }
            case 47:    /* SRA A */
            { A(sra(A())); return (8); }

            case 48:    /* SLS B */
            { B(sls(B())); return (8); }
            case 49:    /* SLS C */
            { C(sls(C())); return (8); }
            case 50:    /* SLS D */
            { D(sls(D())); return (8); }
            case 51:    /* SLS E */
            { E(sls(E())); return (8); }
            case 52:    /* SLS H */
            { H(sls(H())); return (8); }
            case 53:    /* SLS L */
            { L(sls(L())); return (8); }
            case 54:    /* SLS (HL) */
            {
                    int hl = HL();
                    pokeb(hl, sls(peekb(hl)));
                    return (15);
                }
            case 55:    /* SLS A */
            { A(sls(A())); return (8); }

            case 56:    /* SRL B */
            { B(srl(B())); return (8); }
            case 57:    /* SRL C */
            { C(srl(C())); return (8); }
            case 58:    /* SRL D */
            { D(srl(D())); return (8); }
            case 59:    /* SRL E */
            { E(srl(E())); return (8); }
            case 60:    /* SRL H */
            { H(srl(H())); return (8); }
            case 61:    /* SRL L */
            { L(srl(L())); return (8); }
            case 62:    /* SRL (HL) */
            {
                    int hl = HL();
                    pokeb(hl, srl(peekb(hl)));
                    return (15);
                }
            case 63:    /* SRL A */
            { A(srl(A())); return (8); }

            case 64:    /* BIT 0,B */
            { bit(0x01, B()); return (8); }
            case 65:    /* BIT 0,C */
            { bit(0x01, C()); return (8); }
            case 66:    /* BIT 0,D */
            { bit(0x01, D()); return (8); }
            case 67:    /* BIT 0,E */
            { bit(0x01, E()); return (8); }
            case 68:    /* BIT 0,H */
            { bit(0x01, H()); return (8); }
            case 69:    /* BIT 0,L */
            { bit(0x01, L()); return (8); }
            case 70:    /* BIT 0,(HL) */
            { bit(0x01, peekb(HL())); return (12); }
            case 71:    /* BIT 0,A */
            { bit(0x01, A()); return (8); }

            case 72:    /* BIT 1,B */
            { bit(0x02, B()); return (8); }
            case 73:    /* BIT 1,C */
            { bit(0x02, C()); return (8); }
            case 74:    /* BIT 1,D */
            { bit(0x02, D()); return (8); }
            case 75:    /* BIT 1,E */
            { bit(0x02, E()); return (8); }
            case 76:    /* BIT 1,H */
            { bit(0x02, H()); return (8); }
            case 77:    /* BIT 1,L */
            { bit(0x02, L()); return (8); }
            case 78:    /* BIT 1,(HL) */
            { bit(0x02, peekb(HL())); return (12); }
            case 79:    /* BIT 1,A */
            { bit(0x02, A()); return (8); }

            case 80:    /* BIT 2,B */
            { bit(0x04, B()); return (8); }
            case 81:    /* BIT 2,C */
            { bit(0x04, C()); return (8); }
            case 82:    /* BIT 2,D */
            { bit(0x04, D()); return (8); }
            case 83:    /* BIT 2,E */
            { bit(0x04, E()); return (8); }
            case 84:    /* BIT 2,H */
            { bit(0x04, H()); return (8); }
            case 85:    /* BIT 2,L */
            { bit(0x04, L()); return (8); }
            case 86:    /* BIT 2,(HL) */
            { bit(0x04, peekb(HL())); return (12); }
            case 87:    /* BIT 2,A */
            { bit(0x04, A()); return (8); }

            case 88:    /* BIT 3,B */
            { bit(0x08, B()); return (8); }
            case 89:    /* BIT 3,C */
            { bit(0x08, C()); return (8); }
            case 90:    /* BIT 3,D */
            { bit(0x08, D()); return (8); }
            case 91:    /* BIT 3,E */
            { bit(0x08, E()); return (8); }
            case 92:    /* BIT 3,H */
            { bit(0x08, H()); return (8); }
            case 93:    /* BIT 3,L */
            { bit(0x08, L()); return (8); }
            case 94:    /* BIT 3,(HL) */
            { bit(0x08, peekb(HL())); return (12); }
            case 95:    /* BIT 3,A */
            { bit(0x08, A()); return (8); }

            case 96:    /* BIT 4,B */
            { bit(0x10, B()); return (8); }
            case 97:    /* BIT 4,C */
            { bit(0x10, C()); return (8); }
            case 98:    /* BIT 4,D */
            { bit(0x10, D()); return (8); }
            case 99:    /* BIT 4,E */
            { bit(0x10, E()); return (8); }
            case 100:   /* BIT 4,H */
            { bit(0x10, H()); return (8); }
            case 101:   /* BIT 4,L */
            { bit(0x10, L()); return (8); }
            case 102:   /* BIT 4,(HL) */
            { bit(0x10, peekb(HL())); return (12); }
            case 103:   /* BIT 4,A */
            { bit(0x10, A()); return (8); }

            case 104:   /* BIT 5,B */
            { bit(0x20, B()); return (8); }
            case 105:   /* BIT 5,C */
            { bit(0x20, C()); return (8); }
            case 106:   /* BIT 5,D */
            { bit(0x20, D()); return (8); }
            case 107:   /* BIT 5,E */
            { bit(0x20, E()); return (8); }
            case 108:   /* BIT 5,H */
            { bit(0x20, H()); return (8); }
            case 109:   /* BIT 5,L */
            { bit(0x20, L()); return (8); }
            case 110:   /* BIT 5,(HL) */
            { bit(0x20, peekb(HL())); return (12); }
            case 111:   /* BIT 5,A */
            { bit(0x20, A()); return (8); }

            case 112:   /* BIT 6,B */
            { bit(0x40, B()); return (8); }
            case 113:   /* BIT 6,C */
            { bit(0x40, C()); return (8); }
            case 114:   /* BIT 6,D */
            { bit(0x40, D()); return (8); }
            case 115:   /* BIT 6,E */
            { bit(0x40, E()); return (8); }
            case 116:   /* BIT 6,H */
            { bit(0x40, H()); return (8); }
            case 117:   /* BIT 6,L */
            { bit(0x40, L()); return (8); }
            case 118:   /* BIT 6,(HL) */
            { bit(0x40, peekb(HL())); return (12); }
            case 119:   /* BIT 6,A */
            { bit(0x40, A()); return (8); }

            case 120:   /* BIT 7,B */
            { bit(0x80, B()); return (8); }
            case 121:   /* BIT 7,C */
            { bit(0x80, C()); return (8); }
            case 122:   /* BIT 7,D */
            { bit(0x80, D()); return (8); }
            case 123:   /* BIT 7,E */
            { bit(0x80, E()); return (8); }
            case 124:   /* BIT 7,H */
            { bit(0x80, H()); return (8); }
            case 125:   /* BIT 7,L */
            { bit(0x80, L()); return (8); }
            case 126:   /* BIT 7,(HL) */
            { bit(0x80, peekb(HL())); return (12); }
            case 127:   /* BIT 7,A */
            { bit(0x80, A()); return (8); }

            case 128:   /* RES 0,B */
            { B(res(0x01, B())); return (8); }
            case 129:   /* RES 0,C */
            { C(res(0x01, C())); return (8); }
            case 130:   /* RES 0,D */
            { D(res(0x01, D())); return (8); }
            case 131:   /* RES 0,E */
            { E(res(0x01, E())); return (8); }
            case 132:   /* RES 0,H */
            { H(res(0x01, H())); return (8); }
            case 133:   /* RES 0,L */
            { L(res(0x01, L())); return (8); }
            case 134:   /* RES 0,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x01, peekb(hl)));
                    return (15);
                }
            case 135:   /* RES 0,A */
            { A(res(0x01, A())); return (8); }

            case 136:   /* RES 1,B */
            { B(res(0x02, B())); return (8); }
            case 137:   /* RES 1,C */
            { C(res(0x02, C())); return (8); }
            case 138:   /* RES 1,D */
            { D(res(0x02, D())); return (8); }
            case 139:   /* RES 1,E */
            { E(res(0x02, E())); return (8); }
            case 140:   /* RES 1,H */
            { H(res(0x02, H())); return (8); }
            case 141:   /* RES 1,L */
            { L(res(0x02, L())); return (8); }
            case 142:   /* RES 1,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x02, peekb(hl)));
                    return (15);
                }
            case 143:   /* RES 1,A */
            { A(res(0x02, A())); return (8); }

            case 144:   /* RES 2,B */
            { B(res(0x04, B())); return (8); }
            case 145:   /* RES 2,C */
            { C(res(0x04, C())); return (8); }
            case 146:   /* RES 2,D */
            { D(res(0x04, D())); return (8); }
            case 147:   /* RES 2,E */
            { E(res(0x04, E())); return (8); }
            case 148:   /* RES 2,H */
            { H(res(0x04, H())); return (8); }
            case 149:   /* RES 2,L */
            { L(res(0x04, L())); return (8); }
            case 150:   /* RES 2,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x04, peekb(hl)));
                    return (15);
                }
            case 151:   /* RES 2,A */
            { A(res(0x04, A())); return (8); }

            case 152:   /* RES 3,B */
            { B(res(0x08, B())); return (8); }
            case 153:   /* RES 3,C */
            { C(res(0x08, C())); return (8); }
            case 154:   /* RES 3,D */
            { D(res(0x08, D())); return (8); }
            case 155:   /* RES 3,E */
            { E(res(0x08, E())); return (8); }
            case 156:   /* RES 3,H */
            { H(res(0x08, H())); return (8); }
            case 157:   /* RES 3,L */
            { L(res(0x08, L())); return (8); }
            case 158:   /* RES 3,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x08, peekb(hl)));
                    return (15);
                }
            case 159:   /* RES 3,A */
            { A(res(0x08, A())); return (8); }

            case 160:   /* RES 4,B */
            { B(res(0x10, B())); return (8); }
            case 161:   /* RES 4,C */
            { C(res(0x10, C())); return (8); }
            case 162:   /* RES 4,D */
            { D(res(0x10, D())); return (8); }
            case 163:   /* RES 4,E */
            { E(res(0x10, E())); return (8); }
            case 164:   /* RES 4,H */
            { H(res(0x10, H())); return (8); }
            case 165:   /* RES 4,L */
            { L(res(0x10, L())); return (8); }
            case 166:   /* RES 4,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x10, peekb(hl)));
                    return (15);
                }
            case 167:   /* RES 4,A */
            { A(res(0x10, A())); return (8); }

            case 168:   /* RES 5,B */
            { B(res(0x20, B())); return (8); }
            case 169:   /* RES 5,C */
            { C(res(0x20, C())); return (8); }
            case 170:   /* RES 5,D */
            { D(res(0x20, D())); return (8); }
            case 171:   /* RES 5,E */
            { E(res(0x20, E())); return (8); }
            case 172:   /* RES 5,H */
            { H(res(0x20, H())); return (8); }
            case 173:   /* RES 5,L */
            { L(res(0x20, L())); return (8); }
            case 174:   /* RES 5,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x20, peekb(hl)));
                    return (15);
                }
            case 175:   /* RES 5,A */
            { A(res(0x20, A())); return (8); }

            case 176:   /* RES 6,B */
            { B(res(0x40, B())); return (8); }
            case 177:   /* RES 6,C */
            { C(res(0x40, C())); return (8); }
            case 178:   /* RES 6,D */
            { D(res(0x40, D())); return (8); }
            case 179:   /* RES 6,E */
            { E(res(0x40, E())); return (8); }
            case 180:   /* RES 6,H */
            { H(res(0x40, H())); return (8); }
            case 181:   /* RES 6,L */
            { L(res(0x40, L())); return (8); }
            case 182:   /* RES 6,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x40, peekb(hl)));
                    return (15);
                }
            case 183:   /* RES 6,A */
            { A(res(0x40, A())); return (8); }

            case 184:   /* RES 7,B */
            { B(res(0x80, B())); return (8); }
            case 185:   /* RES 7,C */
            { C(res(0x80, C())); return (8); }
            case 186:   /* RES 7,D */
            { D(res(0x80, D())); return (8); }
            case 187:   /* RES 7,E */
            { E(res(0x80, E())); return (8); }
            case 188:   /* RES 7,H */
            { H(res(0x80, H())); return (8); }
            case 189:   /* RES 7,L */
            { L(res(0x80, L())); return (8); }
            case 190:   /* RES 7,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, res(0x80, peekb(hl)));
                    return (15);
                }
            case 191:   /* RES 7,A */
            { A(res(0x80, A())); return (8); }

            case 192:   /* SET 0,B */
            { B(set(0x01, B())); return (8); }
            case 193:   /* SET 0,C */
            { C(set(0x01, C())); return (8); }
            case 194:   /* SET 0,D */
            { D(set(0x01, D())); return (8); }
            case 195:   /* SET 0,E */
            { E(set(0x01, E())); return (8); }
            case 196:   /* SET 0,H */
            { H(set(0x01, H())); return (8); }
            case 197:   /* SET 0,L */
            { L(set(0x01, L())); return (8); }
            case 198:   /* SET 0,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x01, peekb(hl)));
                    return (15);
                }
            case 199:   /* SET 0,A */
            { A(set(0x01, A())); return (8); }

            case 200:   /* SET 1,B */
            { B(set(0x02, B())); return (8); }
            case 201:   /* SET 1,C */
            { C(set(0x02, C())); return (8); }
            case 202:   /* SET 1,D */
            { D(set(0x02, D())); return (8); }
            case 203:   /* SET 1,E */
            { E(set(0x02, E())); return (8); }
            case 204:   /* SET 1,H */
            { H(set(0x02, H())); return (8); }
            case 205:   /* SET 1,L */
            { L(set(0x02, L())); return (8); }
            case 206:   /* SET 1,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x02, peekb(hl)));
                    return (15);
                }
            case 207:   /* SET 1,A */
            { A(set(0x02, A())); return (8); }

            case 208:   /* SET 2,B */
            { B(set(0x04, B())); return (8); }
            case 209:   /* SET 2,C */
            { C(set(0x04, C())); return (8); }
            case 210:   /* SET 2,D */
            { D(set(0x04, D())); return (8); }
            case 211:   /* SET 2,E */
            { E(set(0x04, E())); return (8); }
            case 212:   /* SET 2,H */
            { H(set(0x04, H())); return (8); }
            case 213:   /* SET 2,L */
            { L(set(0x04, L())); return (8); }
            case 214:   /* SET 2,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x04, peekb(hl)));
                    return (15);
                }
            case 215:   /* SET 2,A */
            { A(set(0x04, A())); return (8); }

            case 216:   /* SET 3,B */
            { B(set(0x08, B())); return (8); }
            case 217:   /* SET 3,C */
            { C(set(0x08, C())); return (8); }
            case 218:   /* SET 3,D */
            { D(set(0x08, D())); return (8); }
            case 219:   /* SET 3,E */
            { E(set(0x08, E())); return (8); }
            case 220:   /* SET 3,H */
            { H(set(0x08, H())); return (8); }
            case 221:   /* SET 3,L */
            { L(set(0x08, L())); return (8); }
            case 222:   /* SET 3,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x08, peekb(hl)));
                    return (15);
                }
            case 223:   /* SET 3,A */
            { A(set(0x08, A())); return (8); }

            case 224:   /* SET 4,B */
            { B(set(0x10, B())); return (8); }
            case 225:   /* SET 4,C */
            { C(set(0x10, C())); return (8); }
            case 226:   /* SET 4,D */
            { D(set(0x10, D())); return (8); }
            case 227:   /* SET 4,E */
            { E(set(0x10, E())); return (8); }
            case 228:   /* SET 4,H */
            { H(set(0x10, H())); return (8); }
            case 229:   /* SET 4,L */
            { L(set(0x10, L())); return (8); }
            case 230:   /* SET 4,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x10, peekb(hl)));
                    return (15);
                }
            case 231:   /* SET 4,A */
            { A(set(0x10, A())); return (8); }

            case 232:   /* SET 5,B */
            { B(set(0x20, B())); return (8); }
            case 233:   /* SET 5,C */
            { C(set(0x20, C())); return (8); }
            case 234:   /* SET 5,D */
            { D(set(0x20, D())); return (8); }
            case 235:   /* SET 5,E */
            { E(set(0x20, E())); return (8); }
            case 236:   /* SET 5,H */
            { H(set(0x20, H())); return (8); }
            case 237:   /* SET 5,L */
            { L(set(0x20, L())); return (8); }
            case 238:   /* SET 5,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x20, peekb(hl)));
                    return (15);
                }
            case 239:   /* SET 5,A */
            { A(set(0x20, A())); return (8); }

            case 240:   /* SET 6,B */
            { B(set(0x40, B())); return (8); }
            case 241:   /* SET 6,C */
            { C(set(0x40, C())); return (8); }
            case 242:   /* SET 6,D */
            { D(set(0x40, D())); return (8); }
            case 243:   /* SET 6,E */
            { E(set(0x40, E())); return (8); }
            case 244:   /* SET 6,H */
            { H(set(0x40, H())); return (8); }
            case 245:   /* SET 6,L */
            { L(set(0x40, L())); return (8); }
            case 246:   /* SET 6,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x40, peekb(hl)));
                    return (15);
                }
            case 247:   /* SET 6,A */
            { A(set(0x40, A())); return (8); }

            case 248:   /* SET 7,B */
            { B(set(0x80, B())); return (8); }
            case 249:   /* SET 7,C */
            { C(set(0x80, C())); return (8); }
            case 250:   /* SET 7,D */
            { D(set(0x80, D())); return (8); }
            case 251:   /* SET 7,E */
            { E(set(0x80, E())); return (8); }
            case 252:   /* SET 7,H */
            { H(set(0x80, H())); return (8); }
            case 253:   /* SET 7,L */
            { L(set(0x80, L())); return (8); }
            case 254:   /* SET 7,(HL) */
            {
                    int hl = HL();
                    pokeb(hl, set(0x80, peekb(hl)));
                    return (15);
                }
            case 255:   /* SET 7,A */
            { A(set(0x80, A())); return (8); }

            } // end switch

            return 0;
        }

        private int execute_id() {

            REFRESH(1);

            switch (nxtpcb()) {

            case 0: /* NOP */
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:

            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:

            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:

            case 39:
            case 40:

            case 47:
            case 48:
            case 49:
            case 50:
            case 51:

            case 55:
            case 56:

            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
            case 64:
            case 65:
            case 66:
            case 67:

            case 71:
            case 72:
            case 73:
            case 74:
            case 75:

            case 79:
            case 80:
            case 81:
            case 82:
            case 83:

            case 87:
            case 88:
            case 89:
            case 90:
            case 91:

            case 95:

            case 120:
            case 121:
            case 122:
            case 123:

            case 127:
            case 128:
            case 129:
            case 130:
            case 131:

            case 135:
            case 136:
            case 137:
            case 138:
            case 139:

            case 143:
            case 144:
            case 145:
            case 146:
            case 147:

            case 151:
            case 152:
            case 153:
            case 154:
            case 155:

            case 159:
            case 160:
            case 161:
            case 162:
            case 163:

            case 167:
            case 168:
            case 169:
            case 170:
            case 171:

            case 175:
            case 176:
            case 177:
            case 178:
            case 179:

            case 183:
            case 184:
            case 185:
            case 186:
            case 187:

            case 191:
            case 192:
            case 193:
            case 194:
            case 195:
            case 196:
            case 197:
            case 198:
            case 199:
            case 200:
            case 201:
            case 202:

            case 204:
            case 205:
            case 206:
            case 207:
            case 208:
            case 209:
            case 210:
            case 211:
            case 212:
            case 213:
            case 214:
            case 215:
            case 216:
            case 217:
            case 218:
            case 219:
            case 220:
            case 221:
            case 222:
            case 223:
            case 224:

            case 226:

            case 228:

            case 230:
            case 231:
            case 232:

            case 234:
            case 235:
            case 236:
            case 237:
            case 238:
            case 239:
            case 240:
            case 241:
            case 242:
            case 243:
            case 244:
            case 245:
            case 246:
            case 247:
            case 248: {
                    PC(dec16(PC()));
                    REFRESH(-1);
                    return (4);
                }

            case 9: /* ADD ID,BC */
            { ID(add16(ID(), BC())); return (15); }
            case 25: /* ADD ID,DE */
            { ID(add16(ID(), DE())); return (15); }
            case 41: /* ADD ID,ID */
            {
                    int id = ID();
                    ID(add16(id, id));
                    return (15);
                }
            case 57: /* ADD ID,SP */
            { ID(add16(ID(), SP())); return (15); }

            case 33: /* LD ID,nn */
            { ID(nxtpcw()); return (14); }
            case 34: /* LD (nn),ID */
            { pokew(nxtpcw(), ID()); return (20); }
            case 42: /* LD ID,(nn) */
            { ID(peekw(nxtpcw())); return (20); }
            case 35:/* INC ID */
            { ID(inc16(ID())); return (10); }
            case 43:/* DEC ID */
            { ID(dec16(ID())); return (10); }
            case 36:/* INC IDH */
            { IDH(inc8(IDH())); return (8); }
            case 44:/* INC IDL */
            { IDL(inc8(IDL())); return (8); }
            case 52:/* INC (ID+d) */
            {
                    int z = ID_d();
                    pokeb(z, inc8(peekb(z)));
                    return (23);
                }
            case 37:/* DEC IDH */
            { IDH(dec8(IDH())); return (8); }
            case 45:/* DEC IDL */
            { IDL(dec8(IDL())); return (8); }
            case 53:/* DEC (ID+d) */
            {
                    int z = ID_d();
                    pokeb(z, dec8(peekb(z)));
                    return (23);
                }

            case 38: /* LD IDH,n */
            { IDH(nxtpcb()); return (11); }
            case 46: /* LD IDL,n */
            { IDL(nxtpcb()); return (11); }
            case 54: /* LD (ID+d),n */
            { int z = ID_d(); pokeb(z, nxtpcb()); return (19); }

            case 68: /* LD B,IDH */
            { B(IDH()); return (8); }
            case 69: /* LD B,IDL */
            { B(IDL()); return (8); }
            case 70: /* LD B,(ID+d) */
            { B(peekb(ID_d())); return (19); }

            case 76: /* LD C,IDH */
            { C(IDH()); return (8); }
            case 77: /* LD C,IDL */
            { C(IDL()); return (8); }
            case 78: /* LD C,(ID+d) */
            { C(peekb(ID_d())); return (19); }

            case 84: /* LD D,IDH */
            { D(IDH()); return (8); }
            case 85: /* LD D,IDL */
            { D(IDL()); return (8); }
            case 86: /* LD D,(ID+d) */
            { D(peekb(ID_d())); return (19); }

            case 92: /* LD E,IDH */
            { E(IDH()); return (8); }
            case 93: /* LD E,IDL */
            { E(IDL()); return (8); }
            case 94: /* LD E,(ID+d) */
            { E(peekb(ID_d())); return (19); }

            case 96: /* LD IDH,B */
            { IDH(B()); return (8); }
            case 97: /* LD IDH,C */
            { IDH(C()); return (8); }
            case 98: /* LD IDH,D */
            { IDH(D()); return (8); }
            case 99: /* LD IDH,E */
            { IDH(E()); return (8); }
            case 100: /* LD IDH,IDH */
            { return (8); }
            case 101: /* LD IDH,IDL */
            { IDH(IDL()); return (8); }
            case 102: /* LD H,(ID+d) */
            { H(peekb(ID_d())); return (19); }
            case 103: /* LD IDH,A */
            { IDH(A()); return (8); }

            case 104: /* LD IDL,B */
            { IDL(B()); return (8); }
            case 105: /* LD IDL,C */
            { IDL(C()); return (8); }
            case 106: /* LD IDL,D */
            { IDL(D()); return (8); }
            case 107: /* LD IDL,E */
            { IDL(E()); return (8); }
            case 108: /* LD IDL,IDH */
            { IDL(IDH()); return (8); }
            case 109: /* LD IDL,IDL */
            { return (8); }
            case 110: /* LD L,(ID+d) */
            { L(peekb(ID_d())); return (19); }
            case 111: /* LD IDL,A */
            { IDL(A()); return (8); }

            case 112: /* LD (ID+d),B */
            { pokeb(ID_d(), B()); return (19); }
            case 113: /* LD (ID+d),C */
            { pokeb(ID_d(), C()); return (19); }
            case 114: /* LD (ID+d),D */
            { pokeb(ID_d(), D()); return (19); }
            case 115: /* LD (ID+d),E */
            { pokeb(ID_d(), E()); return (19); }
            case 116: /* LD (ID+d),H */
            { pokeb(ID_d(), H()); return (19); }
            case 117: /* LD (ID+d),L */
            { pokeb(ID_d(), L()); return (19); }
            case 119: /* LD (ID+d),A */
            { pokeb(ID_d(), A()); return (19); }

            case 124: /* LD A,IDH */
            { A(IDH()); return (8); }
            case 125: /* LD A,IDL */
            { A(IDL()); return (8); }
            case 126: /* LD A,(ID+d) */
            { A(peekb(ID_d())); return (19); }

            case 132: /* ADD A,IDH */
            { add_a(IDH()); return (8); }
            case 133: /* ADD A,IDL */
            { add_a(IDL()); return (8); }
            case 134: /* ADD A,(ID+d) */
            { add_a(peekb(ID_d())); return (19); }

            case 140: /* ADC A,IDH */
            { adc_a(IDH()); return (8); }
            case 141: /* ADC A,IDL */
            { adc_a(IDL()); return (8); }
            case 142: /* ADC A,(ID+d) */
            { adc_a(peekb(ID_d())); return (19); }

            case 148: /* SUB IDH */
            { sub_a(IDH()); return (8); }
            case 149: /* SUB IDL */
            { sub_a(IDL()); return (8); }
            case 150: /* SUB (ID+d) */
            { sub_a(peekb(ID_d())); return (19); }

            case 156: /* SBC A,IDH */
            { sbc_a(IDH()); return (8); }
            case 157: /* SBC A,IDL */
            { sbc_a(IDL()); return (8); }
            case 158: /* SBC A,(ID+d) */
            { sbc_a(peekb(ID_d())); return (19); }

            case 164: /* AND IDH */
            { and_a(IDH()); return (8); }
            case 165: /* AND IDL */
            { and_a(IDL()); return (8); }
            case 166: /* AND (ID+d) */
            { and_a(peekb(ID_d())); return (19); }

            case 172: /* XOR IDH */
            { xor_a(IDH()); return (8); }
            case 173: /* XOR IDL */
            { xor_a(IDL()); return (8); }
            case 174: /* XOR (ID+d) */
            { xor_a(peekb(ID_d())); return (19); }

            case 180: /* OR IDH */
            { or_a(IDH()); return (8); }
            case 181: /* OR IDL */
            { or_a(IDL()); return (8); }
            case 182: /* OR (ID+d) */
            { or_a(peekb(ID_d())); return (19); }

            case 188: /* CP IDH */
            { cp_a(IDH()); return (8); }
            case 189: /* CP IDL */
            { cp_a(IDL()); return (8); }
            case 190: /* CP (ID+d) */
            { cp_a(peekb(ID_d())); return (19); }

            case 225: /* POP ID */
            { ID(popw()); return (14); }

            case 233: /* JP (ID) */
            { PC(ID()); return (8); }

            case 249: /* LD SP,ID */
            { SP(ID()); return (10); }

            case 203: /* prefix CB */
            {
                    // Get index address (offset sbyte is first)
                    int z = ID_d();
                    // Opcode comes after offset sbyte
                    int op = nxtpcb();
                    execute_id_cb(op, z);
                    // Bit instructions take 20 T states, rest 23
                    return (((op & 0xc0) == 0x40) ? 20 : 23);
                }

            case 227: /* EX (SP),ID */
            {
                    int t = ID();
                    int sp = SP();
                    ID(peekw(sp));
                    pokew(sp, t);
                    return (23);
                }

            case 229:    /* PUSH ID */
            { pushw(ID()); return (15); }

            } // end switch

            return 0;
        }


        private void execute_id_cb(int op, int z) {

            switch (op) {

            case 0: /* RLC B */
            { B(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 1: /* RLC C */
            { C(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 2: /* RLC D */
            { D(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 3: /* RLC E */
            { E(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 4: /* RLC H */
            { H(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 5: /* RLC L */
            { L(op = rlc(peekb(z))); pokeb(z, op); return; }
            case 6: /* RLC (HL) */
            { pokeb(z, rlc(peekb(z))); return; }
            case 7: /* RLC A */
            { A(op = rlc(peekb(z))); pokeb(z, op); return; }

            case 8: /* RRC B */
            { B(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 9: /* RRC C */
            { C(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 10:    /* RRC D */
            { D(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 11:    /* RRC E */
            { E(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 12:    /* RRC H */
            { H(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 13:    /* RRC L */
            { L(op = rrc(peekb(z))); pokeb(z, op); return; }
            case 14:    /* RRC (HL) */
            { pokeb(z, rrc(peekb(z))); return; }
            case 15:    /* RRC A */
            { A(op = rrc(peekb(z))); pokeb(z, op); return; }

            case 16:    /* RL B */
            { B(op = rl(peekb(z))); pokeb(z, op); return; }
            case 17:    /* RL C */
            { C(op = rl(peekb(z))); pokeb(z, op); return; }
            case 18:    /* RL D */
            { D(op = rl(peekb(z))); pokeb(z, op); return; }
            case 19:    /* RL E */
            { E(op = rl(peekb(z))); pokeb(z, op); return; }
            case 20:    /* RL H */
            { H(op = rl(peekb(z))); pokeb(z, op); return; }
            case 21:    /* RL L */
            { L(op = rl(peekb(z))); pokeb(z, op); return; }
            case 22:    /* RL (HL) */
            { pokeb(z, rl(peekb(z))); return; }
            case 23:    /* RL A */
            { A(op = rl(peekb(z))); pokeb(z, op); return; }

            case 24:    /* RR B */
            { B(op = rr(peekb(z))); pokeb(z, op); return; }
            case 25:    /* RR C */
            { C(op = rr(peekb(z))); pokeb(z, op); return; }
            case 26:    /* RR D */
            { D(op = rr(peekb(z))); pokeb(z, op); return; }
            case 27:    /* RR E */
            { E(op = rr(peekb(z))); pokeb(z, op); return; }
            case 28:    /* RR H */
            { H(op = rr(peekb(z))); pokeb(z, op); return; }
            case 29:    /* RR L */
            { L(op = rr(peekb(z))); pokeb(z, op); return; }
            case 30:    /* RR (HL) */
            { pokeb(z, rr(peekb(z))); return; }
            case 31:    /* RR A */
            { A(op = rr(peekb(z))); pokeb(z, op); return; }

            case 32:    /* SLA B */
            { B(op = sla(peekb(z))); pokeb(z, op); return; }
            case 33:    /* SLA C */
            { C(op = sla(peekb(z))); pokeb(z, op); return; }
            case 34:    /* SLA D */
            { D(op = sla(peekb(z))); pokeb(z, op); return; }
            case 35:    /* SLA E */
            { E(op = sla(peekb(z))); pokeb(z, op); return; }
            case 36:    /* SLA H */
            { H(op = sla(peekb(z))); pokeb(z, op); return; }
            case 37:    /* SLA L */
            { L(op = sla(peekb(z))); pokeb(z, op); return; }
            case 38:    /* SLA (HL) */
            { pokeb(z, sla(peekb(z))); return; }
            case 39:    /* SLA A */
            { A(op = sla(peekb(z))); pokeb(z, op); return; }

            case 40:    /* SRA B */
            { B(op = sra(peekb(z))); pokeb(z, op); return; }
            case 41:    /* SRA C */
            { C(op = sra(peekb(z))); pokeb(z, op); return; }
            case 42:    /* SRA D */
            { D(op = sra(peekb(z))); pokeb(z, op); return; }
            case 43:    /* SRA E */
            { E(op = sra(peekb(z))); pokeb(z, op); return; }
            case 44:    /* SRA H */
            { H(op = sra(peekb(z))); pokeb(z, op); return; }
            case 45:    /* SRA L */
            { L(op = sra(peekb(z))); pokeb(z, op); return; }
            case 46:    /* SRA (HL) */
            { pokeb(z, sra(peekb(z))); return; }
            case 47:    /* SRA A */
            { A(op = sra(peekb(z))); pokeb(z, op); return; }

            case 48:    /* SLS B */
            { B(op = sls(peekb(z))); pokeb(z, op); return; }
            case 49:    /* SLS C */
            { C(op = sls(peekb(z))); pokeb(z, op); return; }
            case 50:    /* SLS D */
            { D(op = sls(peekb(z))); pokeb(z, op); return; }
            case 51:    /* SLS E */
            { E(op = sls(peekb(z))); pokeb(z, op); return; }
            case 52:    /* SLS H */
            { H(op = sls(peekb(z))); pokeb(z, op); return; }
            case 53:    /* SLS L */
            { L(op = sls(peekb(z))); pokeb(z, op); return; }
            case 54:    /* SLS (HL) */
            { pokeb(z, sls(peekb(z))); return; }
            case 55:    /* SLS A */
            { A(op = sls(peekb(z))); pokeb(z, op); return; }

            case 56:    /* SRL B */
            { B(op = srl(peekb(z))); pokeb(z, op); return; }
            case 57:    /* SRL C */
            { C(op = srl(peekb(z))); pokeb(z, op); return; }
            case 58:    /* SRL D */
            { D(op = srl(peekb(z))); pokeb(z, op); return; }
            case 59:    /* SRL E */
            { E(op = srl(peekb(z))); pokeb(z, op); return; }
            case 60:    /* SRL H */
            { H(op = srl(peekb(z))); pokeb(z, op); return; }
            case 61:    /* SRL L */
            { L(op = srl(peekb(z))); pokeb(z, op); return; }
            case 62:    /* SRL (HL) */
            { pokeb(z, srl(peekb(z))); return; }
            case 63:    /* SRL A */
            { A(op = srl(peekb(z))); pokeb(z, op); return; }

            case 64:    /* BIT 0,B */
            case 65:    /* BIT 0,B */
            case 66:    /* BIT 0,B */
            case 67:    /* BIT 0,B */
            case 68:    /* BIT 0,B */
            case 69:    /* BIT 0,B */
            case 70:    /* BIT 0,B */
            case 71:    /* BIT 0,B */
            { bit(0x01, peekb(z)); return; }

            case 72:    /* BIT 1,B */
            case 73:    /* BIT 1,B */
            case 74:    /* BIT 1,B */
            case 75:    /* BIT 1,B */
            case 76:    /* BIT 1,B */
            case 77:    /* BIT 1,B */
            case 78:    /* BIT 1,B */
            case 79:    /* BIT 1,B */
            { bit(0x02, peekb(z)); return; }

            case 80:    /* BIT 2,B */
            case 81:    /* BIT 2,B */
            case 82:    /* BIT 2,B */
            case 83:    /* BIT 2,B */
            case 84:    /* BIT 2,B */
            case 85:    /* BIT 2,B */
            case 86:    /* BIT 2,B */
            case 87:    /* BIT 2,B */
            { bit(0x04, peekb(z)); return; }

            case 88:    /* BIT 3,B */
            case 89:    /* BIT 3,B */
            case 90:    /* BIT 3,B */
            case 91:    /* BIT 3,B */
            case 92:    /* BIT 3,B */
            case 93:    /* BIT 3,B */
            case 94:    /* BIT 3,B */
            case 95:    /* BIT 3,B */
            { bit(0x08, peekb(z)); return; }

            case 96:    /* BIT 4,B */
            case 97:    /* BIT 4,B */
            case 98:    /* BIT 4,B */
            case 99:    /* BIT 4,B */
            case 100:   /* BIT 4,B */
            case 101:   /* BIT 4,B */
            case 102:   /* BIT 4,B */
            case 103:   /* BIT 4,B */
            { bit(0x10, peekb(z)); return; }

            case 104:   /* BIT 5,B */
            case 105:   /* BIT 5,B */
            case 106:   /* BIT 5,B */
            case 107:   /* BIT 5,B */
            case 108:   /* BIT 5,B */
            case 109:   /* BIT 5,B */
            case 110:   /* BIT 5,B */
            case 111:   /* BIT 5,B */
            { bit(0x20, peekb(z)); return; }

            case 112:   /* BIT 6,B */
            case 113:   /* BIT 6,B */
            case 114:   /* BIT 6,B */
            case 115:   /* BIT 6,B */
            case 116:   /* BIT 6,B */
            case 117:   /* BIT 6,B */
            case 118:   /* BIT 6,B */
            case 119:   /* BIT 6,B */
            { bit(0x40, peekb(z)); return; }

            case 120:   /* BIT 7,B */
            case 121:   /* BIT 7,B */
            case 122:   /* BIT 7,B */
            case 123:   /* BIT 7,B */
            case 124:   /* BIT 7,B */
            case 125:   /* BIT 7,B */
            case 126:   /* BIT 7,B */
            case 127:   /* BIT 7,B */
            { bit(0x80, peekb(z)); return; }

            case 128:   /* RES 0,B */
            { B(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 129:   /* RES 0,C */
            { C(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 130:   /* RES 0,D */
            { D(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 131:   /* RES 0,E */
            { E(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 132:   /* RES 0,H */
            { H(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 133:   /* RES 0,L */
            { L(op = res(0x01, peekb(z))); pokeb(z, op); return; }
            case 134:   /* RES 0,(HL) */
            { pokeb(z, res(0x01, peekb(z))); return; }
            case 135:   /* RES 0,A */
            { A(op = res(0x01, peekb(z))); pokeb(z, op); return; }

            case 136:   /* RES 1,B */
            { B(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 137:   /* RES 1,C */
            { C(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 138:   /* RES 1,D */
            { D(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 139:   /* RES 1,E */
            { E(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 140:   /* RES 1,H */
            { H(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 141:   /* RES 1,L */
            { L(op = res(0x02, peekb(z))); pokeb(z, op); return; }
            case 142:   /* RES 1,(HL) */
            { pokeb(z, res(0x02, peekb(z))); return; }
            case 143:   /* RES 1,A */
            { A(op = res(0x02, peekb(z))); pokeb(z, op); return; }

            case 144:   /* RES 2,B */
            { B(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 145:   /* RES 2,C */
            { C(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 146:   /* RES 2,D */
            { D(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 147:   /* RES 2,E */
            { E(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 148:   /* RES 2,H */
            { H(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 149:   /* RES 2,L */
            { L(op = res(0x04, peekb(z))); pokeb(z, op); return; }
            case 150:   /* RES 2,(HL) */
            { pokeb(z, res(0x04, peekb(z))); return; }
            case 151:   /* RES 2,A */
            { A(op = res(0x04, peekb(z))); pokeb(z, op); return; }

            case 152:   /* RES 3,B */
            { B(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 153:   /* RES 3,C */
            { C(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 154:   /* RES 3,D */
            { D(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 155:   /* RES 3,E */
            { E(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 156:   /* RES 3,H */
            { H(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 157:   /* RES 3,L */
            { L(op = res(0x08, peekb(z))); pokeb(z, op); return; }
            case 158:   /* RES 3,(HL) */
            { pokeb(z, res(0x08, peekb(z))); return; }
            case 159:   /* RES 3,A */
            { A(op = res(0x08, peekb(z))); pokeb(z, op); return; }

            case 160:   /* RES 4,B */
            { B(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 161:   /* RES 4,C */
            { C(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 162:   /* RES 4,D */
            { D(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 163:   /* RES 4,E */
            { E(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 164:   /* RES 4,H */
            { H(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 165:   /* RES 4,L */
            { L(op = res(0x10, peekb(z))); pokeb(z, op); return; }
            case 166:   /* RES 4,(HL) */
            { pokeb(z, res(0x10, peekb(z))); return; }
            case 167:   /* RES 4,A */
            { A(op = res(0x10, peekb(z))); pokeb(z, op); return; }

            case 168:   /* RES 5,B */
            { B(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 169:   /* RES 5,C */
            { C(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 170:   /* RES 5,D */
            { D(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 171:   /* RES 5,E */
            { E(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 172:   /* RES 5,H */
            { H(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 173:   /* RES 5,L */
            { L(op = res(0x20, peekb(z))); pokeb(z, op); return; }
            case 174:   /* RES 5,(HL) */
            { pokeb(z, res(0x20, peekb(z))); return; }
            case 175:   /* RES 5,A */
            { A(op = res(0x20, peekb(z))); pokeb(z, op); return; }

            case 176:   /* RES 6,B */
            { B(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 177:   /* RES 6,C */
            { C(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 178:   /* RES 6,D */
            { D(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 179:   /* RES 6,E */
            { E(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 180:   /* RES 6,H */
            { H(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 181:   /* RES 6,L */
            { L(op = res(0x40, peekb(z))); pokeb(z, op); return; }
            case 182:   /* RES 6,(HL) */
            { pokeb(z, res(0x40, peekb(z))); return; }
            case 183:   /* RES 6,A */
            { A(op = res(0x40, peekb(z))); pokeb(z, op); return; }

            case 184:   /* RES 7,B */
            { B(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 185:   /* RES 7,C */
            { C(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 186:   /* RES 7,D */
            { D(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 187:   /* RES 7,E */
            { E(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 188:   /* RES 7,H */
            { H(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 189:   /* RES 7,L */
            { L(op = res(0x80, peekb(z))); pokeb(z, op); return; }
            case 190:   /* RES 7,(HL) */
            { pokeb(z, res(0x80, peekb(z))); return; }
            case 191:   /* RES 7,A */
            { A(op = res(0x80, peekb(z))); pokeb(z, op); return; }

            case 192:   /* SET 0,B */
            { B(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 193:   /* SET 0,C */
            { C(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 194:   /* SET 0,D */
            { D(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 195:   /* SET 0,E */
            { E(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 196:   /* SET 0,H */
            { H(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 197:   /* SET 0,L */
            { L(op = set(0x01, peekb(z))); pokeb(z, op); return; }
            case 198:   /* SET 0,(HL) */
            { pokeb(z, set(0x01, peekb(z))); return; }
            case 199:   /* SET 0,A */
            { A(op = set(0x01, peekb(z))); pokeb(z, op); return; }

            case 200:   /* SET 1,B */
            { B(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 201:   /* SET 1,C */
            { C(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 202:   /* SET 1,D */
            { D(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 203:   /* SET 1,E */
            { E(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 204:   /* SET 1,H */
            { H(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 205:   /* SET 1,L */
            { L(op = set(0x02, peekb(z))); pokeb(z, op); return; }
            case 206:   /* SET 1,(HL) */
            { pokeb(z, set(0x02, peekb(z))); return; }
            case 207:   /* SET 1,A */
            { A(op = set(0x02, peekb(z))); pokeb(z, op); return; }

            case 208:   /* SET 2,B */
            { B(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 209:   /* SET 2,C */
            { C(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 210:   /* SET 2,D */
            { D(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 211:   /* SET 2,E */
            { E(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 212:   /* SET 2,H */
            { H(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 213:   /* SET 2,L */
            { L(op = set(0x04, peekb(z))); pokeb(z, op); return; }
            case 214:   /* SET 2,(HL) */
            { pokeb(z, set(0x04, peekb(z))); return; }
            case 215:   /* SET 2,A */
            { A(op = set(0x04, peekb(z))); pokeb(z, op); return; }

            case 216:   /* SET 3,B */
            { B(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 217:   /* SET 3,C */
            { C(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 218:   /* SET 3,D */
            { D(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 219:   /* SET 3,E */
            { E(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 220:   /* SET 3,H */
            { H(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 221:   /* SET 3,L */
            { L(op = set(0x08, peekb(z))); pokeb(z, op); return; }
            case 222:   /* SET 3,(HL) */
            { pokeb(z, set(0x08, peekb(z))); return; }
            case 223:   /* SET 3,A */
            { A(op = set(0x08, peekb(z))); pokeb(z, op); return; }

            case 224:   /* SET 4,B */
            { B(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 225:   /* SET 4,C */
            { C(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 226:   /* SET 4,D */
            { D(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 227:   /* SET 4,E */
            { E(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 228:   /* SET 4,H */
            { H(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 229:   /* SET 4,L */
            { L(op = set(0x10, peekb(z))); pokeb(z, op); return; }
            case 230:   /* SET 4,(HL) */
            { pokeb(z, set(0x10, peekb(z))); return; }
            case 231:   /* SET 4,A */
            { A(op = set(0x10, peekb(z))); pokeb(z, op); return; }

            case 232:   /* SET 5,B */
            { B(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 233:   /* SET 5,C */
            { C(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 234:   /* SET 5,D */
            { D(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 235:   /* SET 5,E */
            { E(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 236:   /* SET 5,H */
            { H(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 237:   /* SET 5,L */
            { L(op = set(0x20, peekb(z))); pokeb(z, op); return; }
            case 238:   /* SET 5,(HL) */
            { pokeb(z, set(0x20, peekb(z))); return; }
            case 239:   /* SET 5,A */
            { A(op = set(0x20, peekb(z))); pokeb(z, op); return; }

            case 240:   /* SET 6,B */
            { B(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 241:   /* SET 6,C */
            { C(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 242:   /* SET 6,D */
            { D(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 243:   /* SET 6,E */
            { E(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 244:   /* SET 6,H */
            { H(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 245:   /* SET 6,L */
            { L(op = set(0x40, peekb(z))); pokeb(z, op); return; }
            case 246:   /* SET 6,(HL) */
            { pokeb(z, set(0x40, peekb(z))); return; }
            case 247:   /* SET 6,A */
            { A(op = set(0x40, peekb(z))); pokeb(z, op); return; }

            case 248:   /* SET 7,B */
            { B(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 249:   /* SET 7,C */
            { C(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 250:   /* SET 7,D */
            { D(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 251:   /* SET 7,E */
            { E(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 252:   /* SET 7,H */
            { H(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 253:   /* SET 7,L */
            { L(op = set(0x80, peekb(z))); pokeb(z, op); return; }
            case 254:   /* SET 7,(HL) */
            { pokeb(z, set(0x80, peekb(z))); return; }
            case 255:   /* SET 7,A */
            { A(op = set(0x80, peekb(z))); pokeb(z, op); return; }

            } // end switch
        }


        private int in_bc() {
            int ans = inb(BC());

            setZ(ans == 0);
            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setPV(parity[ans]);
            setN(false);
            setH(false);

            return ans;
        }

        /** Add with carry - alters all flags (CHECKED) */
        private void
        adc_a(int b) {
            int a = A();
            int c = Cset() ? 1 : 0;
            int wans = a + b + c;
            int ans = wans & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setC((wans & 0x100) != 0);
            setPV(((a ^ ~b) & (a ^ ans) & 0x80) != 0);
            setH((((a & 0x0f) + (b & 0x0f) + c) & F_H) != 0);
            setN(false);

            A(ans);
        }

        /** Add - alters all flags (CHECKED) */
        private void
        add_a(int b) {
            int a = A();
            int wans = a + b;
            int ans = wans & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setC((wans & 0x100) != 0);
            setPV(((a ^ ~b) & (a ^ ans) & 0x80) != 0);
            setH((((a & 0x0f) + (b & 0x0f)) & F_H) != 0);
            setN(false);

            A(ans);
        }

        /** Subtract with carry - alters all flags (CHECKED) */
        private void
        sbc_a(int b) {
            int a = A();
            int c = Cset() ? 1 : 0;
            int wans = a - b - c;
            int ans = wans & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setC((wans & 0x100) != 0);
            setPV(((a ^ b) & (a ^ ans) & 0x80) != 0);
            setH((((a & 0x0f) - (b & 0x0f) - c) & F_H) != 0);
            setN(true);

            A(ans);
        }

        /** Subtract - alters all flags (CHECKED) */
        private void
        sub_a(int b) {
            int a = A();
            int wans = a - b;
            int ans = wans & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setC((wans & 0x100) != 0);
            setPV(((a ^ b) & (a ^ ans) & 0x80) != 0);
            setH((((a & 0x0f) - (b & 0x0f)) & F_H) != 0);
            setN(true);

            A(ans);
        }

        /** Rotate Left - alters H N C 3 5 flags (CHECKED) */
        private void
        rlc_a() {
            int ans = A();
            bool c = (ans & 0x80) != 0;

            if (c) {
                ans = (ans << 1) | 0x01;
            }
            else {
                ans <<= 1;
            }
            ans &= 0xff;

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setH(false);
            setC(c);

            A(ans);
        }

        /** Rotate Right - alters H N C 3 5 flags (CHECKED) */
        private void
        rrc_a() {
            int ans = A();
            bool c = (ans & 0x01) != 0;

            if (c) {
                ans = (ans >> 1) | 0x80;
            }
            else {
                ans >>= 1;
            }

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setH(false);
            setC(c);

            A(ans);
        }

        /** Rotate Left through Carry - alters H N C 3 5 flags (CHECKED) */
        private void
        rl_a() {
            int ans = A();
            bool c = (ans & 0x80) != 0;

            if (Cset()) {
                ans = (ans << 1) | 0x01;
            }
            else {
                ans <<= 1;
            }

            ans &= 0xff;

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setH(false);
            setC(c);

            A(ans);
        }

        /** Rotate Right through Carry - alters H N C 3 5 flags (CHECKED) */
        private void
        rr_a() {
            int ans = A();
            bool c = (ans & 0x01) != 0;

            if (Cset()) {
                ans = (ans >> 1) | 0x80;
            }
            else {
                ans >>= 1;
            }

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setH(false);
            setC(c);

            A(ans);
        }

        /** Compare - alters all flags (CHECKED) */
        private void
        cp_a(int b) {
            int a = A();
            int wans = a - b;
            int ans = wans & 0xff;

            setS((ans & F_S) != 0);
            set3((b & F_3) != 0);
            set5((b & F_5) != 0);
            setN(true);
            setZ(ans == 0);
            setC((wans & 0x100) != 0);
            setH((((a & 0x0f) - (b & 0x0f)) & F_H) != 0);
            setPV(((a ^ b) & (a ^ ans) & 0x80) != 0);
        }

        /** Bitwise and - alters all flags (CHECKED) */
        private void
        and_a(int b) {
            int ans = A() & b;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setH(true);
            setPV(parity[ans]);
            setZ(ans == 0);
            setN(false);
            setC(false);

            A(ans);
        }

        /** Bitwise or - alters all flags (CHECKED) */
        private void
        or_a(int b) {
            int ans = A() | b;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setH(false);
            setPV(parity[ans]);
            setZ(ans == 0);
            setN(false);
            setC(false);

            A(ans);
        }

        /** Bitwise exclusive or - alters all flags (CHECKED) */
        private void
        xor_a(int b) {
            int ans = (A() ^ b) & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setH(false);
            setPV(parity[ans]);
            setZ(ans == 0);
            setN(false);
            setC(false);

            A(ans);
        }

        /** Negate (Two's complement) - alters all flags (CHECKED) */
        private void
        neg_a() {
            int t = A();

            A(0);
            sub_a(t);
        }

        /** One's complement - alters N H 3 5 flags (CHECKED) */
        private void
        cpl_a() {
            int ans = A() ^ 0xff;

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setH(true);
            setN(true);

            A(ans);
        }

        /** Decimal Adjust Accumulator - alters all flags (CHECKED) */
        private void
        daa_a() {
            int ans = A();
            int incr = 0;
            bool carry = Cset();

            if ((Hset()) || ((ans & 0x0f) > 0x09)) {
                incr |= 0x06;
            }
            if (carry || (ans > 0x9f) || ((ans > 0x8f) && ((ans & 0x0f) > 0x09))) {
                incr |= 0x60;
            }
            if (ans > 0x99) {
                carry = true;
            }
            if (Nset()) {
                sub_a(incr);
            }
            else {
                add_a(incr);
            }

            ans = A();

            setC(carry);
            setPV(parity[ans]);
        }

        /** Load a with i - (NOT CHECKED) */
        private void
        ld_a_i() {
            int ans = I();

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ(ans == 0);
            setPV(IFF2());
            setH(false);
            setN(false);

            A(ans);
        }

        /** Load a with r - (NOT CHECKED) */
        private void
        ld_a_r() {
            int ans = R();

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ(ans == 0);
            setPV(IFF2());
            setH(false);
            setN(false);

            A(ans);
        }

        /** Rotate right through a and (hl) - (NOT CHECKED) */
        private void
        rrd_a() {
            int ans = A();
            int t = peekb(HL());
            int q = t;

            t = (t >> 4) | (ans << 4);
            ans = (ans & 0xf0) | (q & 0x0f);
            pokeb(HL(), t);

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ(ans == 0);
            setPV(IFF2());
            setH(false);
            setN(false);

            A(ans);
        }

        /** Rotate left through a and (hl) - (NOT CHECKED) */
        private void
        rld_a() {
            int ans = A();
            int t = peekb(HL());
            int q = t;

            t = (t << 4) | (ans & 0x0f);
            ans = (ans & 0xf0) | (q >> 4);
            pokeb(HL(), (t & 0xff));

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ(ans == 0);
            setPV(IFF2());
            setH(false);
            setN(false);

            A(ans);
        }

        /** Test bit - alters all but C flag (CHECKED) */
        private void
        bit(int b, int r) {
            bool bitSet = ((r & b) != 0);

            setN(false);
            setH(true);
            set3((r & F_3) != 0);
            set5((r & F_5) != 0);
            setS((b == F_S) ? bitSet : false);
            setZ(!bitSet);
            setPV(!bitSet);
        }

        /** Set carry flag - alters N H 3 5 C flags (CHECKED) */
        private void
        scf() {
            int ans = A();

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setH(false);
            setC(true);
        }

        /** Complement carry flag - alters N 3 5 C flags (CHECKED) */
        private void
        ccf() {
            int ans = A();

            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setN(false);
            setC(Cset() ? false : true);
        }

        /** Rotate left - alters all flags (CHECKED) */
        private int
        rlc(int ans) {
            bool c = (ans & 0x80) != 0;

            if (c) {
                ans = (ans << 1) | 0x01;
            }
            else {
                ans <<= 1;
            }
            ans &= 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Rotate right - alters all flags (CHECKED) */
        private int
        rrc(int ans) {
            bool c = (ans & 0x01) != 0;

            if (c) {
                ans = (ans >> 1) | 0x80;
            }
            else {
                ans >>= 1;
            }

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Rotate left through carry - alters all flags (CHECKED) */
        private int
        rl(int ans) {
            bool c = (ans & 0x80) != 0;

            if (Cset()) {
                ans = (ans << 1) | 0x01;
            }
            else {
                ans <<= 1;
            }
            ans &= 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Rotate right through carry - alters all flags (CHECKED) */
        private int
        rr(int ans) {
            bool c = (ans & 0x01) != 0;

            if (Cset()) {
                ans = (ans >> 1) | 0x80;
            }
            else {
                ans >>= 1;
            }

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Shift Left Arithmetically - alters all flags (CHECKED) */
        private int
        sla(int ans) {
            bool c = (ans & 0x80) != 0;
            ans = (ans << 1) & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Shift Left and Set - alters all flags (CHECKED) */
        private int
        sls(int ans) {
            bool c = (ans & 0x80) != 0;
            ans = ((ans << 1) | 0x01) & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Shift Right Arithmetically - alters all flags (CHECKED) */
        private int
        sra(int ans) {
            bool c = (ans & 0x01) != 0;
            ans = (ans >> 1) | (ans & 0x80);

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Shift Right Logically - alters all flags (CHECKED) */
        private int srl(int ans) {
            bool c = (ans & 0x01) != 0;
            ans = ans >> 1;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(parity[ans]);
            setH(false);
            setN(false);
            setC(c);

            return (ans);
        }

        /** Decrement - alters all but C flag (CHECKED) */
        private int dec8(int ans) {
            bool pv = (ans == 0x80);
            bool h = (((ans & 0x0f) - 1) & F_H) != 0;
            ans = (ans - 1) & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(pv);
            setH(h);
            setN(true);

            return (ans);
        }

        /** Increment - alters all but C flag (CHECKED) */
        private int inc8(int ans) {
            bool pv = (ans == 0x7f);
            bool h = (((ans & 0x0f) + 1) & F_H) != 0;
            ans = (ans + 1) & 0xff;

            setS((ans & F_S) != 0);
            set3((ans & F_3) != 0);
            set5((ans & F_5) != 0);
            setZ((ans) == 0);
            setPV(pv);
            setH(h);
            setN(false);

            return (ans);
        }

        /** Add with carry - (NOT CHECKED) */
        private int adc16(int a, int b) {
            int c = Cset() ? 1 : 0;
            int lans = a + b + c;
            int ans = lans & 0xffff;

            setS((ans & (F_S << 8)) != 0);
            set3((ans & (F_3 << 8)) != 0);
            set5((ans & (F_5 << 8)) != 0);
            setZ((ans) == 0);
            setC((lans & 0x10000) != 0);
            setPV(((a ^ ~b) & (a ^ ans) & 0x8000) != 0);
            setH((((a & 0x0fff) + (b & 0x0fff) + c) & 0x1000) != 0);
            setN(false);

            return (ans);
        }

        /** Add - (NOT CHECKED) */
        private int add16(int a, int b) {
            int lans = a + b;
            int ans = lans & 0xffff;

            set3((ans & (F_3 << 8)) != 0);
            set5((ans & (F_5 << 8)) != 0);
            setC((lans & 0x10000) != 0);
            setH((((a & 0x0fff) + (b & 0x0fff)) & 0x1000) != 0);
            setN(false);

            return (ans);
        }

        /** Add with carry - (NOT CHECKED) */
        private int sbc16(int a, int b) {
            int c = Cset() ? 1 : 0;
            int lans = a - b - c;
            int ans = lans & 0xffff;

            setS((ans & (F_S << 8)) != 0);
            set3((ans & (F_3 << 8)) != 0);
            set5((ans & (F_5 << 8)) != 0);
            setZ((ans) == 0);
            setC((lans & 0x10000) != 0);
            setPV(((a ^ b) & (a ^ ans) & 0x8000) != 0);
            setH((((a & 0x0fff) - (b & 0x0fff) - c) & 0x1000) != 0);
            setN(true);

            return (ans);
        }

        /** EXX */
        public void exx() {
            int t;

            t = HL();
            HL(_HL_);
            _HL_ = t;

            t = DE();
            DE(_DE_);
            _DE_ = t;

            t = BC();
            BC(_BC_);
            _BC_ = t;
        }

        /** EX AF,AF' */
        public void ex_af_af() {
            int t;
            t = AF(); AF(_AF_); _AF_ = t;
        }

        /** Quick Increment : no flags */
        int inc16(int a) { return (a + 1) & 0xffff; }

        /** Quick Decrement : no flags */
        int dec16(int a) { return (a - 1) & 0xffff; }
        int qdec8(int a) { return (a - 1) & 0xff; }

        /** Bit toggling */
        int res(int bit, int val) { return val & ~bit; }
        int set(int bit, int val) { return val | bit; }
    }
}