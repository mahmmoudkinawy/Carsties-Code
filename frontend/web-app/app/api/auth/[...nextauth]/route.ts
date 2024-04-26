import DuendeIdentityServer6 from 'next-auth/providers/duende-identity-server6';
import { NextAuthOptions } from 'next-auth';
import NextAuth from 'next-auth/next';
import { authOptions } from '../utils/authOptions';

const handler = NextAuth(authOptions);
export { handler as GET, handler as POST };
