﻿using ItzWarty.Networking;
using ItzWarty.Threading;
using NMockito;
using System.Net.Sockets;
using Xunit;

namespace Dargon.Services.Networking.Server.Phases {
   public class IndeterminatePhaseTests : NMockitoInstance {
      private const int kPort = 21337;

      private readonly IndeterminatePhase testObj;

      [Mock] private readonly IThreadingProxy threadingProxy = null;
      [Mock] private readonly INetworkingProxy networkingProxy = null;
      [Mock(Tracking.Untracked)] private readonly IServiceConfiguration configuration = null;
      [Mock] private readonly IPhaseFactory phaseFactory = null;
      [Mock] private readonly IContext context = null;
      [Mock] private readonly ITcpEndPoint connectEndpoint = null;

      public IndeterminatePhaseTests() {
         testObj = new IndeterminatePhase(threadingProxy, networkingProxy, phaseFactory, configuration, context);

         When(configuration.Port).ThenReturn(kPort);
         When(networkingProxy.CreateLoopbackEndPoint(kPort)).ThenReturn(connectEndpoint);
      }

      [Fact]
      public void HandleUpdateGuestPathTest() {
         var clientSocket = CreateMock<IConnectedSocket>();
         var guestPhase = CreateMock<IPhase>();

         When(networkingProxy.CreateListenerSocket(kPort)).ThenThrow(new SocketException());
         When(networkingProxy.CreateConnectedSocket(connectEndpoint)).ThenThrow(new SocketException()).ThenReturn(clientSocket);
         When(phaseFactory.CreateGuestPhase(clientSocket)).ThenReturn(guestPhase);

         testObj.HandleUpdate();

         Verify(networkingProxy, Once()).CreateLoopbackEndPoint(kPort);
         Verify(networkingProxy, Times(2)).CreateListenerSocket(kPort);
         Verify(networkingProxy, Times(2)).CreateConnectedSocket(connectEndpoint);
         Verify(threadingProxy, Once()).Sleep(Any<int>());
         Verify(phaseFactory, Once()).CreateGuestPhase(clientSocket);
         Verify(context, Once()).Transition(guestPhase);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void HandleUpdateHostPathTest() {
         var listenerSocket = CreateMock<IListenerSocket>();
         var hostPhase = CreateMock<IPhase>();

         When(networkingProxy.CreateListenerSocket(kPort)).ThenThrow(new SocketException()).ThenReturn(listenerSocket);
         When(networkingProxy.CreateConnectedSocket(connectEndpoint)).ThenThrow(new SocketException());
         When(phaseFactory.CreateHostPhase(listenerSocket)).ThenReturn(hostPhase);

         testObj.HandleUpdate();

         Verify(networkingProxy, Once()).CreateLoopbackEndPoint(kPort);
         Verify(networkingProxy, Times(2)).CreateListenerSocket(kPort);
         Verify(networkingProxy, Once()).CreateConnectedSocket(connectEndpoint);
         Verify(threadingProxy, Once()).Sleep(Any<int>());
         Verify(phaseFactory, Once()).CreateHostPhase(listenerSocket);
         Verify(context, Once()).Transition(hostPhase);
         VerifyNoMoreInteractions();
      }

      [Fact]
      public void DisposeDoesNothing() {
         testObj.Dispose();
         VerifyNoMoreInteractions();
      }
   }
}
