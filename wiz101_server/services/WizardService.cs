using Open101.Net;
using Open101.Serializer;
using SerializerPlayground;
using wiz101_server.sockets;

namespace wiz101_server.services
{
    class WizardServiceHandler : InGameMetaService, WIZARD_12_Protocol.Handler
    {

        public WizardServiceHandler(GameSocket _sock) : base(_sock) { }

        public bool NetHandleCrownBalance(WIZARD_12_Protocol.MSG_CROWNBALANCE _msg)
        {
            m_socket.Send(
                new WIZARD_12_Protocol.MSG_CROWNBALANCE
                {
                    m_characterID = new GID(),
                    m_failure = 0,
                    m_totalCrowns = 1_234_567_890
                }
            );

            return true;
        }

        public bool NetHandleRequestPrivacyOptions(WIZARD_12_Protocol.MSG_REQUESTPRIVACYOPTIONS _msg)
        {
            m_socket.Send(
                new WIZARD_12_Protocol.MSG_RESPONSEPRIVACYOPTIONS
                {
                    m_allowFriendRequest = 1,
                    m_allowFriendTeleport = 1,
                    m_allowHatchRequest = 1,
                    m_allowPartyInvites = 1,
                    m_allowTradeRequest = 1,
                    m_hidePVPEnemyChat = 0,
                    m_limitHomeToFriends = 0
                }
            );

            return true;
        }

    }
}
