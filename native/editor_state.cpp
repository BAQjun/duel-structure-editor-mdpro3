#include "core/duel.h"
#include "core/field.h"
#include "core/card.h"
#include "core/interpreter.h"
#include "core/effect.h"
#include "core/editor.h"
#include <algorithm>
#include <sstream>
#include <vector>
static std::string State(intptr_t h,bool attachments=true) {
 auto* d=reinterpret_cast<duel*>(h);auto* f=d->game_field;
 std::ostringstream extra;
 extra<<f->infos.turn_id<<':'<<int(f->infos.turn_player)<<':'<<f->infos.phase<<':'<<d->editor_revision;
 for(int p=0;p<2;++p)extra<<':'<<f->core.summon_count[p]<<':'<<int(f->core.extra_summon[p]);
 for(int p=0;p<3;++p) {
  for(const auto* map:{&f->core.effect_count_code[p],&f->core.effect_count_code_duel[p],&f->core.effect_count_code_chain[p]}) {
   std::vector<std::pair<uint32_t,int32_t>> counts(map->begin(),map->end());std::sort(counts.begin(),counts.end());
   extra<<'|';for(const auto& entry:counts)extra<<entry.first<<','<<entry.second<<';';
  }
 }
 for(int player=0;player<2;++player)for(auto loc:{LOCATION_DECK,LOCATION_HAND,LOCATION_MZONE,LOCATION_SZONE,LOCATION_GRAVE,LOCATION_REMOVED,LOCATION_EXTRA})for(int index=0;index<255;++index) {
  auto* card=f->get_field_card(player,loc,index);
  if(card)extra<<'/'<<player<<','<<loc<<','<<index<<','<<card->status<<','<<card->turnid<<','<<card->summon_info<<','<<int(card->summon_player);
 }
 if(attachments){std::vector<card*> sorted(d->cards.begin(),d->cards.end());std::sort(sorted.begin(),sorted.end(),[](card* a,card* b){return a->cardid<b->cardid;});for(auto* c:sorted)if(c->current.location){for(const auto& entry:c->counters)extra<<"C"<<c->cardid<<","<<entry.first<<","<<entry.second;for(auto* material:c->xyz_materials)extra<<"M"<<c->cardid<<","<<material->cardid<<","<<material->data.code<<","<<int(material->owner);}}
 std::vector<card*> summonCards(d->cards.begin(),d->cards.end());std::sort(summonCards.begin(),summonCards.end(),[](card* a,card* b){return a->cardid<b->cardid;});for(auto* c:summonCards)if(c->current.location&&!c->editor_summon_materials.empty()){extra<<"H"<<c->cardid;for(const auto& m:c->editor_summon_materials)extra<<":"<<m.card_id<<","<<m.code<<","<<m.owner<<","<<m.source_player<<","<<m.source_location<<","<<m.source_sequence<<","<<m.source_position<<","<<m.destination_player<<","<<m.destination_location<<","<<m.destination_sequence<<","<<m.destination_position;}
 extra<<d->random.state();extra<<":"<<d->lua->call_depth<<":"<<d->lua->coroutines.size();
 for(const auto& unit:f->core.units)extra<<"U"<<unit.type<<","<<unit.step<<","<<unit.arg1<<","<<unit.arg2<<","<<unit.arg3<<","<<unit.arg4<<","<<(unit.peffect?unit.peffect->id:0);
 for(const auto& unit:f->core.subunits)extra<<"S"<<unit.type<<","<<unit.step<<","<<unit.arg1<<","<<unit.arg2<<","<<unit.arg3<<","<<unit.arg4<<","<<(unit.peffect?unit.peffect->id:0);
 std::vector<effect*> effects(d->effects.begin(),d->effects.end());std::sort(effects.begin(),effects.end(),[](const effect* a,const effect* b){return a->id<b->id;});
 for(auto* e:effects)extra<<"E"<<e->id<<","<<e->code<<","<<e->type<<","<<(e->is_flag(EFFECT_FLAG_FUNC_VALUE)?0:e->value)<<","<<e->flag[0]<<","<<e->flag[1]<<","<<e->reset_flag<<","<<e->reset_count<<","<<int(e->count_limit)<<","<<e->range<<","<<(e->handler?e->handler->cardid:0)<<","<<(e->owner?e->owner->cardid:0)<<","<<e->editor_kind;

 std::string state=extra.str();std::vector<uint8_t> buf(1024*1024);
 auto len=query_field_info(h,buf.data());state.append(reinterpret_cast<char*>(buf.data()),len);
 for(int p=0;p<2;++p)for(auto loc:{LOCATION_DECK,LOCATION_HAND,LOCATION_MZONE,LOCATION_SZONE,LOCATION_GRAVE,LOCATION_REMOVED,LOCATION_EXTRA}) {
  len=query_field_card(h,p,loc,0xf81fff,buf.data(),0);
  state.append(reinterpret_cast<char*>(buf.data()),len);
 }
 return state;
}
extern "C" {
OCGCORE_API uint64_t editor_state_hash(intptr_t h){auto s=State(h);uint64_t result=1469598103934665603ULL;for(unsigned char c:s){result^=c;result*=1099511628211ULL;}return result;}
OCGCORE_API uint64_t editor_state_hash_legacy(intptr_t h){auto s=State(h,false);uint64_t result=1469598103934665603ULL;for(unsigned char c:s){result^=c;result*=1099511628211ULL;}return result;}
OCGCORE_API int editor_prepare_initial_state(intptr_t h){auto* d=reinterpret_cast<duel*>(h);auto* f=d->game_field;if(!d->editor_enabled || f->infos.turn_id!=0)return 0;for(const auto& u:f->core.subunits)if(u.type!=PROCESSOR_TURN && !(u.type==PROCESSOR_ADJUST && u.step==0) && !(d->editor_opening && u.type==PROCESSOR_DRAW && u.step==0))return 0;f->core.units.splice(f->core.units.begin(),f->core.subunits);return 1;}
OCGCORE_API uint32_t editor_turn_id(intptr_t h){return reinterpret_cast<duel*>(h)->game_field->infos.turn_id;}
OCGCORE_API uint32_t editor_phase(intptr_t h){return reinterpret_cast<duel*>(h)->game_field->infos.phase;}
OCGCORE_API uint32_t editor_command_size(){return sizeof(editor_command);}
OCGCORE_API uint32_t editor_card_info_size(){return sizeof(editor_card_info);}
}

extern "C" OCGCORE_API uint32_t editor_turn_player(intptr_t h){return reinterpret_cast<duel*>(h)->game_field->infos.turn_player;}
